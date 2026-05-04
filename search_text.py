import re

with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    content = f.read()

marker = "Qual teste quer fazer agora"
pos = content.find(marker)

if pos == -1:
    print("Marker not found!")
else:
    print(f"Found marker at position {pos}")
    
    # Get 5000 characters after the marker
    after = content[pos:pos+5000]
    
    # Try to extract messages (look for "content":" patterns)
    messages = re.findall(r'"content"\s*:\s*"([^"]{100,800})"', after)
    
    print(f"\nFound {len(messages)} message fragments after marker:\n")
    
    for i, msg in enumerate(messages[:10], 1):  # Show first 10
        # Unescape
        msg = msg.replace('\\n', '\n').replace('\\"', '"').replace('\\\\', '\\')
        print(f"[{i}]")
        print(msg[:500])
        print("-"*80)
