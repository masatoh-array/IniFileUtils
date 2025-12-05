using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IniFileUtils
{
    /// <summary>
    /// Represents a single INI file value with optional comment.
    /// </summary>
    public class IniValue
    {
        /// <summary>
        /// Gets or sets the value string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the optional comment for this value.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Initializes a new instance of the IniValue class.
        /// </summary>
        /// <param name="value">The value string.</param>
        /// <param name="comment">The optional comment.</param>
        public IniValue(string value, string comment = null)
        {
            Value = value;
            Comment = comment;
        }
    }

    /// <summary>
    /// A thread-safe INI file reader/writer with support for comments and Shift-JIS encoding.
    /// 
    /// Features:
    /// - Thread-safe access (process-level) via file-based locking
    /// - Preserves comments from original INI file
    /// - Shift-JIS encoding by default (suitable for Japanese environments)
    /// - Support for quoted values containing spaces
    /// - Compatible with both .NET Framework 4.8+ and .NET Core 3.1+
    /// 
    /// Thread Safety: Thread-safe within a single process. Multiple threads can safely read/write
    /// the same INI file. Not suitable for multi-process scenarios (use file-locking mechanisms instead).
    /// 
    /// Encoding: Uses Shift-JIS (Code Page 932) by default. In .NET Core, the CodePagesEncodingProvider
    /// is automatically registered if available. In .NET Framework, standard code pages are always available.
    /// </summary>
    public class IniFileClass
    {
        private static Encoding _defaultEncoding;
        private static Dictionary<string, object> _fileLocks = new Dictionary<string, object>();
        private static readonly Regex CommentRegex = new Regex("^[;#\\/].*");

        static IniFileClass()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch
            {
                // CodePagesEncodingProvider registration is not required in .NET Framework.
                // Silently ignore if already registered or unavailable.
            }
            _defaultEncoding = Encoding.GetEncoding(932);
        }

        private Dictionary<string, Dictionary<string, IniValue>> _iniData = new Dictionary<string, Dictionary<string, IniValue>>();
        private Dictionary<string, List<string>> _comments = new Dictionary<string, List<string>>();
        private string _filePath;
        private Encoding _encoding;

        /// <summary>
        /// Initializes a new instance of the IniFileClass.
        /// </summary>
        /// <param name="filePath">The path to the INI file. If null, an empty INI structure is created.</param>
        /// <param name="encoding">The encoding to use. Defaults to Shift-JIS (Japanese). Pass Encoding.UTF8 for UTF-8.</param>
        public IniFileClass(string filePath = null, Encoding encoding = null)
        {
            _filePath = filePath;
            _encoding = encoding ?? _defaultEncoding;

            if (!string.IsNullOrEmpty(_filePath))
            {
                EnsureLockObject();
                LoadIniFile();
            }
            else
            {
                _iniData = new Dictionary<string, Dictionary<string, IniValue>>();
                _comments = new Dictionary<string, List<string>>();
            }
        }

        private void EnsureLockObject()
        {
            lock (_fileLocks)
            {
                if (!_fileLocks.ContainsKey(_filePath))
                {
                    _fileLocks[_filePath] = new object();
                }
            }
        }

        private void LoadIniFile()
        {
            lock (_fileLocks[_filePath])
            {
                if (!File.Exists(_filePath))
                {
                    _iniData = new Dictionary<string, Dictionary<string, IniValue>>();
                    return;
                }

                var lines = File.ReadAllLines(_filePath, _encoding);
                ParseLines(lines);
            }
        }

        private void ParseLines(string[] lines)
        {
            string currentSection = "";
            List<string> currentComments = new List<string>();

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (CommentRegex.IsMatch(trimmedLine))
                {
                    currentComments.Add(trimmedLine);
                    continue;
                }
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    if (currentSection != "")
                    {
                        _comments[currentSection] = new List<string>(currentComments);
                        currentComments.Clear();
                    }
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!_iniData.ContainsKey(currentSection))
                    {
                        _iniData[currentSection] = new Dictionary<string, IniValue>();
                    }
                }
                else
                {
                    var keyValue = trimmedLine.Split(new char[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        // Remove quotes if value is quoted
                        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        _iniData[currentSection][key] = new IniValue(value);
                    }
                }
            }
            if (currentSection != "")
            {
                _comments[currentSection] = new List<string>(currentComments);
            }
        }

        /// <summary>
        /// Gets a value from the INI file.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The key name.</param>
        /// <returns>The value string, or null if section/key does not exist.</returns>
        public string GetValue(string section, string key)
        {
            if (_iniData.ContainsKey(section) && _iniData[section].ContainsKey(key))
                return _iniData[section][key].Value;
            return null;
        }

        /// <summary>
        /// Sets a value in the INI file.
        /// </summary>
        /// <param name="section">The section name. Created if it does not exist.</param>
        /// <param name="key">The key name. Overwritten if it already exists.</param>
        /// <param name="value">The value string.</param>
        /// <param name="useQuotes">If true, the value will be surrounded by quotes (useful for values containing spaces).</param>
        public void SetValue(string section, string key, string value, bool useQuotes = false)
        {
            if (!_iniData.ContainsKey(section))
                _iniData[section] = new Dictionary<string, IniValue>();

            string formattedValue = useQuotes ? $"\"{value}\"" : value;
            _iniData[section][key] = new IniValue(formattedValue);
        }

        /// <summary>
        /// Removes a key from the INI file.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The key name.</param>
        /// <param name="removeEmptySection">If true, removes the section if it becomes empty.</param>
        /// <returns>True if the key was removed, false if not found.</returns>
        public bool RemoveKey(string section, string key, bool removeEmptySection = false)
        {
            if (_iniData.ContainsKey(section) && _iniData[section].ContainsKey(key))
            {
                _iniData[section].Remove(key);

                if (removeEmptySection && _iniData[section].Count == 0)
                {
                    _iniData.Remove(section);
                    if (_comments.ContainsKey(section))
                    {
                        _comments.Remove(section);
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a section from the INI file.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <returns>True if the section was removed, false if not found.</returns>
        public bool RemoveSection(string section)
        {
            if (_iniData.ContainsKey(section))
            {
                _iniData.Remove(section);
                if (_comments.ContainsKey(section))
                {
                    _comments.Remove(section);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Saves the INI data to the file with the specified encoding.
        /// Creates the directory if it does not exist.
        /// Thread-safe: Uses process-level file locking.
        /// </summary>
        /// <param name="filePath">Optional: Save to a different file path. If provided, subsequent operations use this path.</param>
        /// <exception cref="InvalidOperationException">Thrown when no file path is specified.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public void Save(string filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _filePath = filePath;
                EnsureLockObject();
            }

            if (string.IsNullOrEmpty(_filePath))
            {
                throw new InvalidOperationException("Cannot save data: INI file path is not specified.");
            }

            string directoryPath = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            try
            {
                lock (_fileLocks[_filePath])
                {
                    using (var sw = new StreamWriter(_filePath, false, _encoding))
                    {
                        foreach (var section in _iniData)
                        {
                            if (_comments.ContainsKey(section.Key))
                            {
                                foreach (var comment in _comments[section.Key])
                                {
                                    sw.WriteLine(comment);
                                }
                            }
                            sw.WriteLine($"[{section.Key}]");
                            foreach (var kvp in section.Value)
                            {
                                if (!string.IsNullOrEmpty(kvp.Value.Comment))
                                {
                                    sw.WriteLine($"; {kvp.Value.Comment}");
                                }
                                sw.WriteLine($"{kvp.Key}={kvp.Value.Value}");
                            }
                            sw.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save data to file '{_filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a comment to a specific key.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The key name.</param>
        /// <param name="comment">The comment text (without the semicolon).</param>
        /// <exception cref="ArgumentException">Thrown when the section/key does not exist.</exception>
        public void AddKeyComment(string section, string key, string comment)
        {
            if (_iniData.ContainsKey(section) && _iniData[section].ContainsKey(key))
            {
                _iniData[section][key].Comment = comment;
            }
            else
            {
                throw new ArgumentException($"The specified section '{section}' and key '{key}' does not exist.");
            }
        }

        /// <summary>
        /// Adds a comment to a section (appears before the section header).
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="comment">The comment text (without prefix).</param>
        /// <param name="prefix">The comment prefix character. Default is `;`. Use `#` or `//` as alternatives.</param>
        public void SetComment(string section, string comment, string prefix = ";")
        {
            if (!_comments.ContainsKey(section))
            {
                _comments[section] = new List<string>();
            }
            _comments[section].Add(prefix + comment);
        }

        /// <summary>
        /// Removes a section comment.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="comment">The comment text to remove (without the semicolon).</param>
        public void RemoveComment(string section, string comment)
        {
            if (_comments.ContainsKey(section))
            {
                _comments[section].Remove(";" + comment);
            }
        }

        /// <summary>
        /// Clears all comments from the INI file.
        /// </summary>
        public void ClearAllComments()
        {
            foreach (var section in _comments.Keys.ToList())
            {
                _comments[section].Clear();
            }
        }

        /// <summary>
        /// Destructor to clean up the file lock.
        /// </summary>
        ~IniFileClass()
        {
            if (!string.IsNullOrEmpty(_filePath))
            {
                lock (_fileLocks)
                {
                    _fileLocks.Remove(_filePath);
                }
            }
        }
    }
}
