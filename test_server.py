import socket

host = '127.0.0.1'
port = 8888

server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.bind((host, port))
server.listen(1)

print(f"Servidor Python rodando em {host}:{port}")
print("Aguardando conexão...")

while True:
    client, addr = server.accept()
    print(f"✓ Cliente conectado: {addr}")
    
    try:
        data = client.recv(1024).decode('utf-8')
        print(f"✓ Recebido: {data.strip()}")
        
        response = "OK\n"
        client.send(response.encode('utf-8'))
        print(f"✓ Enviado: {response.strip()}")
    except Exception as e:
        print(f"✗ Erro: {e}")
    finally:
        client.close()
        print("Cliente desconectado\n")
