import json

file_path = r'C:\Users\Ulisses\.aws\[aws-profile-switcher]_fabiojunior11@gmail.com\amazonq\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json'

with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

collections = data.get('collections', [])

for coll in collections:
    if coll.get('name') == 'tabs':
        tabs = coll.get('data', [])
        
        for tab in tabs:
            conversations = tab.get('conversations', [])
            
            for conv in conversations:
                messages = conv.get('messages', [])
                
                if len(messages) > 0:
                    # Save last 10 messages to file
                    with open('last_messages.txt', 'w', encoding='utf-8') as out:
                        out.write(f"Total messages: {len(messages)}\n")
                        out.write("="*80 + "\n\n")
                        
                        for msg in messages[-10:]:
                            msg_type = msg.get('type', 'unknown')
                            body = msg.get('body', '')
                            timestamp = msg.get('timestamp', '')
                            
                            out.write(f"[{msg_type.upper()}] {timestamp}\n")
                            out.write(body + "\n")
                            out.write("-"*80 + "\n\n")
                    
                    print(f"Saved last 10 messages to last_messages.txt")
