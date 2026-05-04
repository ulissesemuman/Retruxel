import json

with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

collections = data.get('collections', [])
print(f"Found {len(collections)} collections\n")

for coll in collections:
    name = coll.get('name', 'unknown')
    items = coll.get('data', [])
    print(f"Collection: {name} ({len(items)} items)")
    
    if name == 'conversationHistory':
        print("\nLast 5 messages:")
        for msg in items[-5:]:
            role = msg.get('role', 'unknown')
            content = msg.get('content', '')
            timestamp = msg.get('timestamp', '')
            
            if len(content) > 400:
                content = content[:400] + "..."
            
            print(f"\n[{role.upper()}] {timestamp}")
            print(content)
            print("-"*80)
