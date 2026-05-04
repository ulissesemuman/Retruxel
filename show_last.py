import json

# Read the chat history file
with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

messages = data.get('conversationHistory', [])
total = len(messages)

print(f"Total messages: {total}")
print("\n" + "="*80 + "\n")

# Show last 10 messages
for msg in messages[-10:]:
    role = msg.get('role', 'unknown')
    timestamp = msg.get('timestamp', '')
    content = msg.get('content', '')
    
    # Truncate content
    if len(content) > 300:
        content = content[:300] + "..."
    
    print(f"[{role.upper()}] {timestamp}")
    print(content)
    print("-"*80)
