-- Retruxel LiveLink for Mesen 2
local socket = require("socket")
local server = socket.bind("*", 8888)
server:settimeout(0)

 

local client = nil

-- Detect console type
local consoleType = emu.getState().consoleType
emu.log("Retruxel: Console type: " .. tostring(consoleType))

-- Base64 encoding
local b64chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
function base64Encode(data)
    return ((data:gsub('.', function(x) 
        local r,b='',x:byte()
        for i=8,1,-1 do r=r..(b%2^i-b%2^(i-1)>0 and '1' or '0') end
        return r;
    end)..'0000'):gsub('%d%d%d?%d?%d?%d?', function(x)
        if (#x < 6) then return '' end
        local c=0
        for i=1,6 do c=c+(x:sub(i,i)=='1' and 2^(6-i) or 0) end
        return b64chars:sub(c+1,c+1)
    end)..({ '', '==', '=' })[#data%3+1])
end

function readVramByte(addr)
    if consoleType == "Nes" then
        return emu.read(addr, emu.memType.nesPpuMemory)
    elseif consoleType == "Snes" then
        return emu.read(addr, emu.memType.snesVideoRam)
    elseif consoleType == "Sms" or consoleType == "GameGear" or consoleType == "Sg1000" then
        return emu.read(addr, emu.memType.smsVideoRam)
    elseif consoleType == "Gameboy" or consoleType == "GameboyColor" then
        return emu.read(addr, emu.memType.gbVideoRam)
    else
        return 0
    end
end

-- Read VRAM in chunks to avoid overloading
function readVramChunk(startAddr, length, chunkSize)
    local data = {}
    local remaining = length
    local currentAddr = startAddr
    
    emu.log("Retruxel: Starting chunked read - addr: " .. startAddr .. ", total: " .. length .. " bytes")
    
    while remaining > 0 do
        local toRead = math.min(chunkSize, remaining)
        for i = 0, toRead - 1 do
            table.insert(data, readVramByte(currentAddr + i))
        end
        currentAddr = currentAddr + toRead
        remaining = remaining - toRead
        emu.log("Retruxel: Read chunk - progress: " .. #data .. "/" .. length .. " bytes")
    end
    
    emu.log("Retruxel: Chunked read complete - total: " .. #data .. " bytes")
    return data
end

function verificarSocket()
    if not client then
        client = server:accept()
        if client then
            client:settimeout(0)
            emu.log("Retruxel: Client connected")
        end
    end
    
    if client then
        local line, err = client:receive()
        
        if not err then
            local linePreview = line:sub(1, 50)
            emu.log("Retruxel: Received: " .. linePreview)
            
            if line:match("^READ_VRAM:") then
                emu.log("Retruxel: Matched READ_VRAM")
                local addr, len = line:match("^READ_VRAM:(%d+):(%d+)$")
                if addr and len then
                    addr = tonumber(addr)
                    len = tonumber(len)
                    emu.log("Retruxel: Reading VRAM - addr: " .. addr .. ", len: " .. len)
                    
                    -- Read in chunks of 512 bytes to avoid overload
                    local data = readVramChunk(addr, len, 512)
                    
                    local bytes = string.char(table.unpack(data))
                    local b64 = base64Encode(bytes)
                    emu.log("Retruxel: Sending " .. #b64 .. " bytes (base64)")
                    client:send(b64 .. "\n")
                end
            elseif line:match("^READ_MEM:") then
                emu.log("Retruxel: Matched READ_MEM")
                local addr, len = line:match("^READ_MEM:(%d+):(%d+)$")
                if addr and len then
                    addr = tonumber(addr)
                    len = tonumber(len)
                    emu.log("Retruxel: Reading Memory - addr: " .. addr .. ", len: " .. len)
                    
                    local data = {}
                    for i = 0, len - 1 do
                        table.insert(data, emu.read(addr + i, emu.memType.cpu))
                    end
                    
                    local bytes = string.char(table.unpack(data))
                    local b64 = base64Encode(bytes)
                    emu.log("Retruxel: Sending " .. #b64 .. " bytes (base64)")
                    client:send(b64 .. "\n")
                end
            elseif line:match("^READ_CRAM:") then
                emu.log("Retruxel: Matched READ_CRAM")
                local len = line:match("^READ_CRAM:(%d+)$")
                if len then
                    len = tonumber(len)
                    emu.log("Retruxel: Reading CRAM - len: " .. len .. ", console: " .. consoleType)
                    
                    local data = {}
                    if consoleType == "Sms" or consoleType == "Sg1000" then
                        -- SMS/SG-1000: 6-bit RGB, 1 byte per color
                        for i = 0, len - 1 do
                            table.insert(data, emu.read(i, emu.memType.smsPaletteRam, false))
                        end
                    elseif consoleType == "GameGear" then
                        -- Game Gear: 12-bit RGB, 2 bytes per color
                        for i = 0, (len * 2) - 1 do
                            table.insert(data, emu.read(i, emu.memType.smsPaletteRam, false))
                        end
                    elseif consoleType == "Snes" then
                        -- SNES CGRAM: 256 colors × 2 bytes = 512 bytes
                        for i = 0, (len * 2) - 1 do
                            table.insert(data, emu.read(i, emu.memType.snesCgRam, false))
                        end
                    elseif consoleType == "Gameboy" or consoleType == "GameboyColor" then
                        -- GB/GBC palette: Read from palette RAM directly
                        -- GB: 0xFF47-0xFF49 (BG, OBJ0, OBJ1)
                        -- GBC: 0xFF68-0xFF6B (BCPS/BCPD, OCPS/OCPD)
                        if consoleType == "GameboyColor" then
                            -- GBC: Read from palette RAM (8 BG palettes + 8 OBJ palettes = 64 colors × 2 bytes)
                            for i = 0, (len * 2) - 1 do
                                table.insert(data, emu.read(i, emu.memType.gbcBootRom, false))
                            end
                        else
                            -- GB: Simple grayscale palettes (3 bytes)
                            for i = 0, len - 1 do
                                table.insert(data, 0xFF)
                                table.insert(data, 0xFF)
                            end
                        end
                    end
                    
                    emu.log("Retruxel: CRAM read complete - " .. #data .. " bytes")
                    local bytes = string.char(table.unpack(data))
                    local b64 = base64Encode(bytes)
                    emu.log("Retruxel: Sending " .. #b64 .. " bytes (base64)")
                    client:send(b64 .. "\n")
                end
            elseif line:match("^READ_VDP_REGS$") then
                emu.log("Retruxel: Matched READ_VDP_REGS")
                -- Read VDP registers for SMS/GG/SG-1000 (TMS9918/VDP)
                -- Registers 0-10 are standard
                local data = {}
                if consoleType == "Sms" or consoleType == "GameGear" or consoleType == "Sg1000" then
                    -- SMS VDP has 11 registers (0-10)
                    for i = 0, 10 do
                        -- Try to read VDP register via I/O port or state
                        -- Mesen may expose this via debug state
                        table.insert(data, 0) -- Placeholder - need to find correct API
                    end
                end
                
                local bytes = string.char(table.unpack(data))
                local b64 = base64Encode(bytes)
                emu.log("Retruxel: Sending VDP registers - " .. #b64 .. " bytes (base64)")
                client:send(b64 .. "\n")
            elseif line:match("^GET_SCREEN$") then
                emu.log("Retruxel: Matched GET_SCREEN")
                local screenData = emu.getScreenBuffer()
                
                if screenData then
                    local pixelCount = #screenData
                    emu.log("Retruxel: Screen buffer size: " .. pixelCount .. " pixels")
                    
                    -- Get screen dimensions
                    local screenSize = emu.getScreenSize()
                    emu.log("Retruxel: Screen dimensions: " .. screenSize.width .. "x" .. screenSize.height)
                    
                    -- Convert RGB table to RGBA bytes in chunks
                    local chunks = {}
                    local chunkSize = 2000
                    
                    for chunkStart = 1, pixelCount, chunkSize do
                        local chunkEnd = math.min(chunkStart + chunkSize - 1, pixelCount)
                        local bytes = {}
                        
                        for i = chunkStart, chunkEnd do
                            local rgb = screenData[i]
                            local r = (rgb >> 16) & 0xFF
                            local g = (rgb >> 8) & 0xFF
                            local b = rgb & 0xFF
                            table.insert(bytes, r)
                            table.insert(bytes, g)
                            table.insert(bytes, b)
                            table.insert(bytes, 255)
                        end
                        
                        table.insert(chunks, string.char(table.unpack(bytes)))
                    end
                    
                    local bytesStr = table.concat(chunks)
                    emu.log("Retruxel: Converted to " .. #bytesStr .. " bytes (RGBA)")
                    
                    -- Encode to base64
                    local b64 = base64Encode(bytesStr)
                    emu.log("Retruxel: Encoded to " .. #b64 .. " bytes (base64)")
                    
                    -- Switch to blocking mode for large data transfer
                    client:settimeout(10)
                    
                    -- Send size first
                    client:send(#b64 .. "\n")
                    
                    -- Send all data at once (blocking mode will handle it)
                    local sent, err = client:send(b64 .. "\n")
                    
                    if sent then
                        emu.log("Retruxel: Sent " .. sent .. " bytes")
                    else
                        emu.log("Retruxel: Send error: " .. tostring(err))
                    end
                    
                    -- Switch back to non-blocking mode
                    client:settimeout(0)
                else
                    emu.log("Retruxel: Failed to get screen buffer")
                    client:send("ERROR\n")
                end
            else
                emu.log("Retruxel: Unknown command: " .. line)
                emu.log("Retruxel: Sending OK")
                client:send("OK\n")
            end
        elseif err == "closed" then
            emu.log("Retruxel: Client disconnected")
            client = nil
        end
    end
end

emu.addEventCallback(verificarSocket, emu.eventType.startFrame)
emu.addEventCallback(verificarSocket, emu.eventType.endFrame)
emu.addEventCallback(verificarSocket, emu.eventType.inputPolled)
emu.addEventCallback(verificarSocket, emu.eventType.cpuExec)
