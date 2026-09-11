namespace Xease.Audio
{
    /// <summary>
    /// 音频元数据
    /// </summary>
    public class AudioRawFile<T> where T : new()
    {
        public T Content { get; private set; }
        public string Path { get; private set; }

        public static implicit operator T(AudioRawFile<T> rawFile)
        {
            return rawFile == null ? default : rawFile.Content;
        }

        public static AudioRawFile<T> Load(string path)
        {
            try
            {
                var handle = Audio.LoadRawFileSync(path);
                if (handle == null)
                {
                    return null;
                }
                var text = handle.GetRawFileText();
                // RawFile 只认 JSON 文本；.kab 本身是 JSON，若被写成 Unity YAML（以 % 开头）则拒绝
                if (string.IsNullOrEmpty(text))
                {
                    Audio.LogError($"[Audio] Load rawfile \"{path}\" error, message: empty file.");
                    return null;
                }
                if (text[0] == '%')
                {
                    Audio.LogError($"[Audio] Load rawfile \"{path}\" error, message: expected JSON, got YAML.");
                    return null;
                }
                var audioRawFile = new AudioRawFile<T>
                {
                    Path = handle.GetRawFilePath(),
                    Content = new T()
                };
                Audio.Deserialize(text, audioRawFile.Content);
                return audioRawFile;
            }
            catch (System.Exception ex)
            {
                Audio.LogError($"[Audio] Load rawfile \"{path}\" error, message: {ex.Message}");
                return null;
            }
            finally
            {
                Audio.Release(path);
            }
        }

        public void Unload()
        {
            Content = default;
            Path = null;
        }
    }
}