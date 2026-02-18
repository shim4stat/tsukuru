using System;
using System.IO;
using Game.Contracts.Save;
using Game.Contracts.Save.Models;
using UnityEngine;

namespace Game.Infrastructure.Save
{
    public sealed class JsonSaveRepository : ISaveRepository
    {
        private const string DefaultFileName = "save.json";

        private readonly string _directoryPath;
        private readonly string _filePath;
        private readonly string _backupFilePath;

        public JsonSaveRepository()
            : this(Application.persistentDataPath, DefaultFileName)
        {
        }

        public JsonSaveRepository(string directoryPath, string fileName = DefaultFileName)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("directoryPath is null or empty.", nameof(directoryPath));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName is null or empty.", nameof(fileName));

            _directoryPath = directoryPath;
            _filePath = Path.Combine(_directoryPath, fileName);
            _backupFilePath = _filePath + ".bak";
        }

        public SaveDataContract LoadOrCreateDefault()
        {
            if (!File.Exists(_filePath))
                return SaveDataMapper.CreateDefaultContract();

            try
            {
                string json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return SaveDataMapper.CreateDefaultContract();

                SaveDataJsonModel model = JsonUtility.FromJson<SaveDataJsonModel>(json);
                if (model == null)
                    throw new InvalidDataException("Failed to parse save JSON.");

                return SaveDataMapper.ToContractNormalized(model);
            }
            catch (Exception ex)
            {
                BackupCorruptedSave(ex);
                return SaveDataMapper.CreateDefaultContract();
            }
        }

        public void Save(SaveDataContract data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!Directory.Exists(_directoryPath))
                Directory.CreateDirectory(_directoryPath);

            SaveDataJsonModel model = SaveDataMapper.ToJsonModel(data);
            string json = JsonUtility.ToJson(model, true);
            File.WriteAllText(_filePath, json);
        }

        private void BackupCorruptedSave(Exception originalException)
        {
            try
            {
                if (File.Exists(_filePath))
                    File.Copy(_filePath, _backupFilePath, true);
            }
            catch (Exception backupException)
            {
                Debug.LogWarning($"Failed to backup corrupted save. path={_filePath}, error={backupException.Message}");
            }

            Debug.LogWarning($"Save load failed. Using defaults. path={_filePath}, error={originalException.Message}");
        }
    }
}
