-- Retruxel LiveLink for mGBA (Game Boy Advance)
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
                -- GBA: BG0 tilemap at 0x06000000
                local data = emu.readBytes(0x06000000, 0x800, emu.memType.vram)
                client:send(data)
            elseif line == "GET_PALETTE" then
                -- GBA: Palette RAM at 0x05000000
                local data = emu.readBytes(0x05000000, 512, emu.memType.palette)
                client:send(data)
            elseif line == "GET_TILES" then
                -- GBA: Tile data at 0x06000000
                local data = emu.readBytes(0x06000000, 0x10000, emu.memType.vram)
                client:send(data)
            end
        end
        client:close()
    end
end

emu.addEventCallback(update, emu.eventType.endFrame)
