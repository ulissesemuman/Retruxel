with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    content = f.read()

# Search for dialog issue
searches = [
    "apareceu di",
    "PaletteImportDialog",
    "otimizador de paleta",
    "importei um asset"
]

for search in searches:
    pos = content.find(search)
    if pos != -1:
        print(f"Found '{search}' at position {pos}")
        snippet = content[max(0, pos-200):pos+500]
        snippet = snippet.replace('\\n', '\n').replace('\\"', '"')
        print(snippet)
        print("\n" + "="*80 + "\n")
        break
