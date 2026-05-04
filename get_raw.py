with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    content = f.read()

marker = "Qual teste quer fazer agora"
pos = content.find(marker)

if pos != -1:
    # Get 3000 characters after marker
    after = content[pos:pos+3000]
    
    # Clean up JSON escaping for readability
    after = after.replace('\\n', '\n').replace('\\"', '"').replace('\\\\', '\\')
    
    print(after)
