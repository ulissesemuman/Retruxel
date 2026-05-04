import json
import sys

# Read the chat history file
with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

# Find the marker message
marker = "Qual teste quer fazer agora? Sugiro #3 (Palette) para validar a ordem de inicialização na cena."
found_marker = False
messages_after = []

for msg in data.get('conversationHistory', []):
    content = msg.get('content', '')
    
    if marker in content:
        found_marker = True
        continue
    
    if found_marker:
        messages_after.append({
            'timestamp': msg.get('timestamp', ''),
            'role': msg.get('role', ''),
            'content': content[:500] if len(content) > 500 else content  # Limit to 500 chars
        })

# Print summary
print(f"Found {len(messages_after)} messages after the marker")
print("\n" + "="*80 + "\n")

for i, msg in enumerate(messages_after, 1):
    print(f"[{i}] {msg['role'].upper()} at {msg['timestamp']}")
    print(msg['content'])
    print("-"*80)
