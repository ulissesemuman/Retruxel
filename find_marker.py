import json

with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

collections = data.get('collections', [])

for coll in collections:
    name = coll.get('name', 'unknown')
    items = coll.get('data', [])
    
    if name == 'tabs':
        print(f"Found {len(items)} tabs\n")
        for tab in items:
            tab_data = tab.get('conversationHistory', [])
            print(f"Tab has {len(tab_data)} messages")
            
            # Find marker
            marker = "Qual teste quer fazer agora"
            found_idx = -1
            
            for i, msg in enumerate(tab_data):
                content = msg.get('content', '')
                if marker in content:
                    found_idx = i
                    print(f"\nFound marker at message {i}")
                    break
            
            if found_idx >= 0:
                print(f"\nShowing {len(tab_data) - found_idx - 1} messages after marker:\n")
                for msg in tab_data[found_idx + 1:]:
                    role = msg.get('role', 'unknown')
                    content = msg.get('content', '')
                    
                    if len(content) > 500:
                        content = content[:500] + "..."
                    
                    print(f"[{role.upper()}]")
                    print(content)
                    print("-"*80)
