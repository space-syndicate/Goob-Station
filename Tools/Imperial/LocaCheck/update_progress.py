# update_progress.py
import os
import json
import sys

def load_checklist(file_path):
    """Загружает файл прогресса"""
    if not os.path.exists(file_path):
        print(f"[i] Файл прогресса не существует, будет создан новый: {file_path}")
        return {}
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        print(f"[!] Ошибка загрузки файла прогресса: {e}")
        return {}

def save_checklist(file_path, checklist):
    """Сохраняет файл прогресса"""
    try:
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(checklist, f, ensure_ascii=False, indent=2)
        return True
    except Exception as e:
        print(f"[!] Ошибка сохранения файла прогресса: {e}")
        return False

def should_skip_line(line):
    """Пропускает пустые строки и комментарии"""
    return not line.strip() or line.strip().startswith('#')

def parse_original_keys(file_path, delimiter):
    """Парсит ключи из файла локализации с сохранением оригинального формата"""
    keys = {}
    if not os.path.exists(file_path):
        print(f"[X] Файл не найден: {file_path}")
        return keys

    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            for line in f:
                # Пропускаем пустые строки и комментарии
                if should_skip_line(line):
                    continue

                # Ищем разделитель в строке
                if delimiter not in line:
                    continue

                # Разделяем на 2 части по первому вхождению разделителя
                parts = line.split(delimiter, 1)
                if len(parts) < 2:
                    continue

                # Сохраняем оригинальный формат без изменений
                path_part = parts[0].rstrip('\n')
                key_part = parts[1].rstrip('\n')
                key_id = f"{path_part}{delimiter}{key_part}"

                keys[key_id] = True

        print(f"[v] Загружено ключей из {os.path.basename(file_path)}: {len(keys)}")
        return keys
    except Exception as e:
        print(f"[X] Ошибка чтения файла {file_path}: {e}")
        return keys

def main():
    print("\n=== ОБНОВЛЕНИЕ ПРОГРЕССА ПЕРЕВОДА ===")
    script_dir = os.path.dirname(os.path.abspath(__file__))
    print(f"[i] Папка скрипта: {script_dir}")

    delimiter = '鎰'

    # Пути к файлам
    original_file = os.path.join(script_dir, 'original.txt')
    progress_file = os.path.join(script_dir, 'translation_progress.json')

    # Список путей, которые нужно добавить в прогресс
    target_paths = {
        "/Locale/en-US/toolshed-commands.ftl",
        "/Locale/en-US/commands.ftl",
        "/Locale/en-US/physics/grid_merging.ftl",
        "/Locale/en-US/userinterface.ftl",
        "/Locale/en-US/view-variables.ftl",
        "/Locale/en-US/entity-category.ftl",
        "/Locale/en-US/midi-commands.ftl",
        "/Locale/en-US/discordRPC.ftl",
        "/Locale/en-US/debug-builtin-connection-screen.ftl",
        "/Locale/en-US/custom-controls.ftl",
        "/Locale/en-US/uploadfolder.ftl",
        "/Locale/en-US/tab-container.ftl",
        "/Locale/en-US/replays.ftl",
        "/Locale/en-US/input.ftl",
        "/Locale/en-US/dev-window.ftl",
        "/Locale/en-US/defaultwindow.ftl",
        "/Locale/en-US/controls.ftl",
        "/Locale/en-US/client-state-commands.ftl",
        "/Locale/en-US/color-naming.ftl",
        "/Locale/en-US/_engine_lib.ftl"

        # Добавьте сюда другие пути по необходимости
    }

    # Проверка существования файлов
    if not os.path.exists(original_file):
        print(f"\n[X] ОШИБКА: Файл не найден: {original_file}")
        print("Убедитесь, что файл original.txt находится в папке скрипта")
        input("\nНажмите Enter для выхода...")
        return

    print(f"[i] Файл исходной локализации: {original_file}")
    print(f"[i] Файл прогресса: {progress_file}")

    # Загрузка данных
    print("\n[i] Загрузка данных...")
    checklist = load_checklist(progress_file)
    original_keys = parse_original_keys(original_file, delimiter)

    # Добавление ключей из указанных путей
    added_count = 0
    updated_count = 0
    print("\n[i] Добавление ключей в прогресс...")

    for key_id in original_keys:
        # Проверяем, начинается ли путь ключа с одного из целевых путей
        for path in target_paths:
            if key_id.startswith(path + delimiter):
                # Если ключ уже есть в чеклисте
                if key_id in checklist:
                    # Обновляем статус только если он не "V"
                    if checklist[key_id] != "V":
                        checklist[key_id] = "V"
                        updated_count += 1
                        print(f"  [↻] Обновлен ключ: {key_id}")
                else:
                    # Добавляем новый ключ с пометкой "V"
                    checklist[key_id] = "V"
                    added_count += 1
                    print(f"  [+] Добавлен ключ: {key_id}")
                break

    # Сохранение результатов
    total_changes = added_count + updated_count
    if total_changes > 0:
        if save_checklist(progress_file, checklist):
            print(f"\n[v] УСПЕХ: Добавлено ключей: {added_count}, Обновлено: {updated_count}")
            print(f"[v] Файл прогресса обновлен: {progress_file}")
        else:
            print("\n[x] ОШИБКА: Не удалось сохранить файл прогресса")
    else:
        print("\n[i] Нет изменений - все ключи уже помечены как переведенные")

    # Пауза перед закрытием
    input("\nНажмите Enter для выхода...")

if __name__ == "__main__":
    main()
