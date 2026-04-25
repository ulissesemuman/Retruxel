-- Test socket bind
emu.log("TESTE: Script iniciou!")

local socket = require("socket")
emu.log("TESTE: Socket library OK!")

local server, err = socket.bind("*", 8888)
if server then
    emu.log("TESTE: Bind na porta 8888 OK!")
    emu.displayMessage(0, "Porta 8888 OK!")
    server:settimeout(0)
else
    emu.log("TESTE: Bind FALHOU: " .. tostring(err))
    emu.displayMessage(0, "Bind FALHOU: " .. tostring(err))
end
