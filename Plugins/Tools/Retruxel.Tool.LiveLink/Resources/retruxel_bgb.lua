-- Retruxel LiveLink for BGB (Game Boy / Game Boy Color)
-- BGB uses a different API - this is a reference implementation
-- BGB's debug console is accessed via TCP on port 8765

local socket = require("socket")
local server = assert(socket.bind("*", 8765))
server:settimeout(0)

function update()
    local client = server:accept()
    if client then
        client:settimeout(10)
        local line = client:receive()
        
        if line then
            if line == "GET_TILEMAP" then
                -- BGB: Background map at 0x9800-0x9BFF
                local data = emu.readBytes(0x9800, 0x400, emu.memType.vram)
                client:send(data)
            elseif line == "GET_PALETTE" then
                -- BGB: Palette data at 0xFF47-0xFF4B
                local data = emu.readBytes(0xFF47, 5, emu.memType.io)
                client:send(data)
            elseif line == "GET_TILES" then
                -- BGB: Tile data at 0x8000-0x97FF
                local data = emu.readBytes(0x8000, 0x1800, emu.memType.vram)
                client:send(data)
            end
        end
        client:close()
    end
end

emu.addEventCallback(update, emu.eventType.endFrame)
