import json

file_path = r'C:\Users\Ulisses\.aws\[aws-profile-switcher]_fabiojunior11@gmail.com\amazonq\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json'

with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Find the conversation messages
collections = data.get('collections', [])

for coll in collections:
    if coll.get('name') == 'tabs':
        tabs = coll.get('data', [])
        
        for tab in tabs:
            conversations = tab.get('conversations', [])
            
            for conv in conversations:
                messages = conv.get('messages', [])
                
                if len(messages) > 0:
                    print(f"Found conversation with {len(messages)} messages")
                    print("="*80)
                    
                    # Show last 30 messages
                    for msg in messages[-30:]:
                        msg_type = msg.get('type', 'unknown')
                        body = msg.get('body', '')
                        timestamp = msg.get('timestamp', '')
                        
                        if len(body) > 800:
                            body = body[:800] + "..."
                        
                        print(f"\n[{msg_type.upper()}] {timestamp}")
                        print(body)
                        print("-"*80)
