-- Test callback
emu.log("TESTE: Script iniciou!")

local socket = require("socket")
local server = socket.bind("*", 8888)
server:settimeout(0)
emu.log("TESTE: Servidor criado na porta 8888")

local client = nil
local frameCount = 0

function verificarSocket()
    frameCount = frameCount + 1
    
    if frameCount == 60 then
        emu.log("TESTE: Callback está rodando! (60 frames)")
        emu.displayMessage(0, "Callback OK!")
    end
    
    if not client then
        client = server:accept()
        if client then
            client:settimeout(0)
            emu.log("TESTE: Cliente conectado!")
            emu.displayMessage(0, "Cliente conectado!")
        end
    end
    
    if client then
        local line, err = client:receive()
        if line then
            emu.log("TESTE: Recebido: " .. line)
            emu.displayMessage(0, "Recebido: " .. line)
            client:send("OK\n")
            emu.log("TESTE: Enviado: OK")
        end
    end
end

emu.addEventCallback(verificarSocket, emu.eventType.startFrame)
emu.log("TESTE: Callback registrado!")
