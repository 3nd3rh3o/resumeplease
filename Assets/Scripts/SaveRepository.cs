using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class SaveSlot
{
    public string id;
    public SaveGameData data;
    public string error;
    public bool recovered;
    public bool IsValid => data != null;
}

public sealed class SaveRepository
{
    public const string AutoSlot = "autosave";
    private readonly string directory;
    public string DirectoryPath => directory;
    public SaveRepository(string directory) => this.directory = Path.GetFullPath(directory);

    private string FilePath(string slot)
    {
        if (slot != AutoSlot && !Guid.TryParseExact(slot, "N", out _))
            throw new ArgumentException("Identifiant de sauvegarde invalide.");
        return Path.Combine(directory, slot + ".json");
    }

    public bool Write(SaveGameData data, string slot, string label, out string error)
    {
        try
        {
            data.slotId = slot;
            data.name = string.IsNullOrWhiteSpace(label) ? data.dayTitle : label.Trim();
            if (data.name.Length > 80) data.name = data.name.Substring(0, 80);
            data.savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            AtomicWrite(FilePath(slot), JsonUtility.ToJson(data, true));
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            error = "Impossible d'écrire la sauvegarde : " + exception.Message;
            return false;
        }
    }

    public SaveSlot Read(string slot)
    {
        var result = new SaveSlot { id = slot };
        try
        {
            string path = FilePath(slot);
            if (TryRead(path, slot, out result.data, out result.error)) return result;
            if (TryRead(path + ".bak", slot, out var backup, out _))
            {
                result.data = backup;
                result.recovered = true;
                result.error = null;
            }
        }
        catch (ArgumentException exception) { result.error = exception.Message; }
        return result;
    }

    private static bool TryRead(string path, string slot, out SaveGameData data, out string error)
    {
        data = null;
        try
        {
            if (!File.Exists(path)) throw new IOException("Fichier absent.");
            if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new IOException("Fichier trop volumineux.");
            var parsed = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path, Encoding.UTF8));
            if (parsed == null || parsed.magic != "ResumePlease" || parsed.version != 1 || parsed.slotId != slot ||
                parsed.day == null || parsed.campaign == null || string.IsNullOrEmpty(parsed.dayId) ||
                !DateTime.TryParse(parsed.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                throw new ArgumentException("Format de sauvegarde invalide ou incompatible.");
            data = parsed;
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            error = exception.Message;
            return false;
        }
    }

    public List<SaveSlot> List()
    {
        var result = new List<SaveSlot>();
        if (!System.IO.Directory.Exists(directory)) return result;
        var ids = new HashSet<string>();
        foreach (string path in System.IO.Directory.GetFiles(directory, "*.json*"))
        {
            string filename = Path.GetFileName(path);
            string id = filename.EndsWith(".json.bak", StringComparison.Ordinal) ? filename.Substring(0, filename.Length - 9)
                : filename.EndsWith(".json", StringComparison.Ordinal) ? Path.GetFileNameWithoutExtension(filename) : null;
            if (id == null || !ids.Add(id)) continue;
            if (id != AutoSlot && !Guid.TryParseExact(id, "N", out _)) continue;
            result.Add(Read(id));
        }
        result.Sort((a, b) => string.CompareOrdinal(b.data?.savedAtUtc, a.data?.savedAtUtc));
        return result;
    }

    public static void AtomicWrite(string path, string content)
    {
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
