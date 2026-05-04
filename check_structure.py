import json

# Read the chat history file
with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

print("JSON keys:", list(data.keys()))
print("\nData type:", type(data))

if isinstance(data, list):
    print(f"List length: {len(data)}")
    if len(data) > 0:
        print("\nFirst item keys:", list(data[0].keys()) if isinstance(data[0], dict) else "Not a dict")
        print("\nLast 3 items:")
        for item in data[-3:]:
            if isinstance(item, dict):
                print(f"  - {item.get('role', 'unknown')}: {str(item.get('content', ''))[:100]}")
elif isinstance(data, dict):
    for key, value in data.items():
        print(f"\n{key}: {type(value)}")
        if isinstance(value, list):
            print(f"  Length: {len(value)}")
