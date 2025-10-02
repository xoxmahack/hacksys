file_path = "db.txt"

def create():
    print("Напиши заметку, которую я сохраню:")
    note = input().strip()
    if note:
        with open(file_path, "a", encoding="utf-8") as f:
            f.write(note + "\n")
        print("Заметка сохранена!")
    else:
        print("Пустая заметка не сохранена.")

def delete():
    notes = get_all_notes()
    if not notes:
        print("Нет заметок для удаления.")
        return

    print("Все заметки:")
    for i, note in enumerate(notes, 1):
        print(f"{i}. {note.strip()}")

        index = int(input("Введи номер заметки для удаления: ")) - 1
        if 0 <= index < len(notes):
            deleted = notes.pop(index)
            with open(file_path, "w", encoding="utf-8") as f:
                f.writelines(note + "\n" for note in notes if note.strip())
            print(f"Удалена заметка: {deleted.strip()}")
        else:
            print("Неверный номер.")


def search():
    query = input("Введи текст для поиска: ").strip().lower()
    if not query:
        print("Пустой запрос.")
        return

    notes = get_all_notes()
    found = [note for note in notes if query in note.lower()]
    if found:
        print("Найденные заметки:")
        for note in found:
            print("- " + note.strip())
    else:
        print("Ничего не найдено.")

def show():
    notes = get_all_notes()
    if notes:
        print("Все заметки:")
        for i, note in enumerate(notes, 1):
            print(f"{i}. {note.strip()}")
    else:
        print("Заметок пока нет.")

def get_all_notes():
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return [line for line in f if line.strip()]
    except FileNotFoundError:
        return []

def close():
    print("Пока! До новых заметок!")
    exit()

def interface():
    print("Сапчик, я — менеджер твоих заметок!")
    while True:
        print('''
Список команд:
1 - создать заметку
2 - удалить заметку
3 - найти заметку
4 - закрыть программу
5 - показать все заметки
Введи номер выбранной команды:''')
        answer = input().strip()
        match answer:
            case "1":
                create()
            case "2":
                delete()
            case "3":
                search()
            case "4":
                close()
            case "5":
                show()
            case _:
                print("Безмозглое подобие человека! Введи число от 1 до 5.")

if __name__ == "__main__":
    interface()
