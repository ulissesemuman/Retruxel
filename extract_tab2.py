with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    content = f.read()

# Find the second tab conversation (after "N縊 apareceu di疝ogo algum")
marker = '"title":"N縊 apareceu di疝ogo algum'
pos = content.find(marker)

if pos != -1:
    print(f"Found second tab at position {pos}\n")
    
    # Get text after this marker
    after = content[pos:pos+8000]
    
    # Find the messages array
    msg_start = after.find('"messages":[')
    if msg_start != -1:
        messages_section = after[msg_start:msg_start+7000]
        
        # Clean up
        messages_section = messages_section.replace('\\n', '\n').replace('\\"', '"')
        
        print(messages_section[:6000])
