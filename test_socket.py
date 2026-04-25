import socket
import time

# Connect to Mesen
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect(("127.0.0.1", 8888))
print("Connected!")

# Send hello
s.sendall(b"HELLO FROM PYTHON\n")
print("Sent: HELLO FROM PYTHON")

# Receive response
response = s.recv(1024)
print(f"Received: {response.decode()}")

# Wait a bit
time.sleep(1)

# Send load ROM command
rom_path = r"F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\test.nes"
s.sendall(f"LOAD_ROM:{rom_path}\n".encode())
print(f"Sent: LOAD_ROM:{rom_path}")

# Receive response
response = s.recv(1024)
print(f"Received: {response.decode()}")

s.close()
print("Done!")
