-- Test socket
emu.log("TESTE: Script iniciou!")

local socket = require("socket")
if socket then
    emu.log("TESTE: Socket library OK!")
    emu.displayMessage(0, "Socket OK!")
else
    emu.log("TESTE: Socket library FALHOU!")
    emu.displayMessage(0, "Socket FALHOU!")
end
