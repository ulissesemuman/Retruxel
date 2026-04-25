-- Retruxel LiveLink for Mesen-S (SNES)
local socket = require("socket")
local server = assert(socket.bind("*", 8888))
server:settimeout(0)

function update()
    local client = server:accept()
    if client then
        client:settimeout(10)
        local line = client:receive()
        
        if line then
            if line == "GET_TILEMAP" then
                local data = emu.readBytes(0x0000, 0x800, emu.memType.snesVram)
                client:send(data)
            elseif line == "GET_PALETTE" then
                local data = emu.readBytes(0x0000, 512, emu.memType.snesCgram)
                client:send(data)
            elseif line == "GET_CHR" then
                local data = emu.readBytes(0x0000, 0x10000, emu.memType.snesVram)
                client:send(data)
            end
        end
        client:close()
    end
end

emu.addEventCallback(update, emu.eventType.endFrame)
