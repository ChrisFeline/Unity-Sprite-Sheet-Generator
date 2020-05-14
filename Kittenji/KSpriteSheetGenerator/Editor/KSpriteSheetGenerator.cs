/*
MIT License

Copyright (c) 2020 Kittenji

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class KSpriteSheetGenerator : EditorWindow
{
    private static bool initiated;
    private static GUIStyle style_BoldLabel;
    private static GUIStyle style_CenterLabel;

    [MenuItem("Kittenji/Sprite Sheet Generator")]
    private static void Init()
    {
        var window = (KSpriteSheetGenerator)EditorWindow.GetWindow(typeof(KSpriteSheetGenerator));
        window.Show();

        window.titleContent = new GUIContent("Sprite Sheet Generator");
        window.minSize = new Vector2(300,400);

        style_BoldLabel = new GUIStyle(EditorStyles.label);
        style_CenterLabel = new GUIStyle(EditorStyles.label);
        style_BoldLabel.fontStyle = FontStyle.Bold;

        style_CenterLabel.alignment = TextAnchor.MiddleCenter;
        style_CenterLabel.normal.textColor = new Color ( 0.35f,0.35f,0.35f, 1 );

        initiated = true;
    }
    
    #region Properties
    // Options
    private TileSize _TileSize = TileSize.Auto;
    private Vector2Int g_Scale = Vector2Int.zero;
    private bool KeepRatio;

    private TileAlignment _TileAlignment = TileAlignment.CustomGrid;
    private int g_Columns = 4;
    private int g_Rows { get { return Mathf.CeilToInt((float)_Textures.Count / (float)Mathf.Max(g_Columns, 1)); }}
    // Storage
    private List<Texture2D> _Textures = new List<Texture2D>();
    // Other
    private bool AllowDuplicates = false;
    private bool TexturesFoldout = true;
    // Privates
    private Vector2 scrollPos = Vector2.zero;
    #endregion

    private void OnGUI()
    {
        if (!initiated) Init();
        DrawSeparator(1, 5.0f);
        
        GUI.enabled = _Textures.Count > 1 && _Textures.FindAll((tex) => tex != null).Count > 1;
        if (GUILayout.Button("Execute!")) {
            // Clear Null and Duplicated
            EditorGUIUtility.AddCursorRect(position, MouseCursor.Orbit);
            _Textures.RemoveAll((txm) => txm == null);
            Execute();
        }
        GUI.enabled = true;

        DrawSeparator(1, 5.0f);
        var allowDup = EditorGUILayout.Toggle("Allow Duplicates", AllowDuplicates);
        if (AllowDuplicates != allowDup && !(AllowDuplicates = allowDup)) _Textures = _Textures.Distinct().ToList();

        DrawSeparator(1, 5.0f);
        _TileSize = (TileSize)EditorGUILayout.EnumPopup("Tile Size", _TileSize);
        GUI.enabled = _TileSize != TileSize.Auto;

        EditorGUILayout.BeginHorizontal();
        var new_Scale = EditorGUILayout.Vector2IntField(new GUIContent("", "Scale in X:With Y:Height"), g_Scale);
        KeepRatio = EditorGUILayout.Toggle(KeepRatio, GUILayout.Width(15));
        EditorGUILayout.EndHorizontal();

        g_Scale = (KeepRatio) ? new Vector2Int(Mathf.Max(1, new_Scale.x + (new_Scale.y - g_Scale.y)), Mathf.Max(1, new_Scale.y + (new_Scale.x - g_Scale.x))) : new Vector2Int(Mathf.Max(1, new_Scale.x), Mathf.Max(1, new_Scale.y));
        
        GUI.enabled = true;

        DrawSeparator(1, 5.0f);
        _TileAlignment = (TileAlignment)EditorGUILayout.EnumPopup("Tile Alignment", _TileAlignment);
        GUI.enabled = _TileAlignment == TileAlignment.CustomGrid;
        if (_TileAlignment == TileAlignment.Vertically) g_Columns = 1;
        if (_TileAlignment == TileAlignment.Horizontally) g_Columns = _Textures.Count;
        g_Columns = Mathf.Max(EditorGUILayout.IntField("Columns", g_Columns), 1);
        GUI.enabled = false;
        EditorGUILayout.IntField("Rows (Auto)", g_Rows);
        GUI.enabled = true;

        DrawSeparator(1, 5.0f);
        var foldRect = GUILayoutUtility.GetRect(new GUIContent("Texture 2D List"), EditorStyles.foldout);
        if (foldRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use ();
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                for(int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var obj = DragAndDrop.objectReferences[i] as Texture2D;
                    if (obj != null && (AllowDuplicates || !_Textures.Contains(obj))) {
                        var idx = _Textures.IndexOf(null);
                        if (idx < 0) _Textures.Add(obj);
                        else _Textures[idx] = obj;
                    }
                }
                Event.current.Use ();
            }
        }

        if (TexturesFoldout = EditorGUI.Foldout(foldRect, TexturesFoldout, "Texture 2D List")) {

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            int newCount = Mathf.Min(Mathf.Max(0, EditorGUILayout.IntField("Count", _Textures.Count)), 100);
            if (GUILayout.Button("C", EditorStyles.miniButton, GUILayout.Width(20))) {_Textures.Clear(); newCount = 0; }
            EditorGUILayout.EndHorizontal();
            
            DrawSeparator(1, 0.0f);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            while (newCount < _Textures.Count)
                _Textures.RemoveAt( _Textures.Count - 1 );
            while (newCount > _Textures.Count)
                _Textures.Add(null);
            
            for(int i = 0; i < _Textures.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label($"Item {i}", GUILayout.Width(50));

                bool move_up   = GUILayout.Button("\u2191", EditorStyles.miniButton, GUILayout.Width(15));
                bool move_down = GUILayout.Button("\u2193", EditorStyles.miniButton, GUILayout.Width(15));
                _Textures[i] = (Texture2D)EditorGUILayout.ObjectField(_Textures[i], typeof(Texture2D), false, GUILayout.MaxHeight(16));
                bool remove = GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
                if (remove) {
                    if (_Textures[i] != null) _Textures[i] = null;
                    else _Textures.RemoveAt(i);
                    return;
                }

                if (move_up || move_down) {
                    int indexA = i;
                    int indexB = Mathf.Max(Mathf.Min(i + ((move_up) ? -1 : 1), _Textures.Count - 1), 0);
                    
                    var tmp = _Textures[indexA];
                    _Textures[indexA] = _Textures[indexB];
                    _Textures[indexB] = tmp;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        DrawSeparator(1, 5.0f);
        DrawNetwork();
        DrawSeparator(1, 5.0f);
    }

    private void Execute() {
        string save_path = EditorUtility.SaveFilePanelInProject("Save New Texture", "New Texture", "png", "Select where to save this new texture.");
        if (string.IsNullOrEmpty(save_path) || string.IsNullOrWhiteSpace(save_path)) return;

        var scal = (_TileSize == TileSize.Auto) ? Validate(_Textures) : g_Scale;
        
        var temp_texture = new Texture2D(scal.x * g_Columns, scal.y * g_Rows);
        Color[] temp_colors = new Color[temp_texture.width * temp_texture.height];
        Color   deft_color  = new Color(0,0,0,0);
        for (int i = 0; i < temp_colors.Length; i++) {
            temp_colors[i] = deft_color;
        }
        temp_texture.SetPixels(temp_colors);

        for (int t = 0; t < _Textures.Count; t++) {
            var _tex = _Textures[t];

            int x_start = scal.x * (t % g_Columns);

            int y_max   = scal.y * ((_Textures.Count - 1) / g_Columns);
            int y_start = scal.y * (t / g_Columns);
            y_start = y_max - y_start;

            for (int x = 0; x < scal.x; x++)
            {
                for (int y = 0; y < scal.y; y++)
                {
                    Color t_color = _tex.GetPixel((int)(((float)x/scal.x) * (float)_tex.width), (int)(((float)y/scal.y) * (float)_tex.height));
                    temp_texture.SetPixel(x_start + x, y_start + y, t_color);
                }
            }
        }
        temp_texture.Apply();

        string savedat = SaveTextureAsPNG(temp_texture, save_path);
        string filenam = Path.GetFileName(save_path);
        EditorUtility.DisplayDialog("File saved.", $"Exported {savedat} as {filenam}", "Okay");
        AssetDatabase.Refresh();
    }

    Vector2Int Validate(List<Texture2D> _texarr) {
        Vector2Int _v2 = Vector2Int.zero;

        for (int i = 0; i < _texarr.Count; i++) {
            var _tx = _texarr[i];
            var _t2 = new Vector2Int(_tx.width, _tx.height);
            if (_t2.x > _v2.x || _t2.y > _v2.y) _v2 = _t2;
        }

        return _v2;
    }

    #region Classes
    public enum TileAlignment {
        CustomGrid,
        Horizontally,
        Vertically
    }
    public enum TileSize {
        Auto,
        Manual
    }
    #endregion

    #region Methods
    public static string SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        File.WriteAllBytes(_fullPath, _bytes);
        return KFile.Format(_bytes.Length);
    }
    // UnityEditor
    void DrawSeparator( int i_height = 1, float space = 0)
    {
        GUILayout.Space(space/2.0f);
        Rect rect = EditorGUILayout.GetControlRect(false, i_height );
        rect.height = i_height;
        EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
        GUILayout.Space(space/2.0f);
    }
    void DrawSeparator( float space = 0, int i_height = 1)
    {
        GUILayout.Space(space/2.0f);
        Rect rect = EditorGUILayout.GetControlRect(false, i_height );
        rect.height = i_height;
        EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
        GUILayout.Space(space/2.0f);
    }

    // Stuff?
    public static class KFile
    {
        static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string Format(int bytes)
        {
            int counter = 0;
            float number = (float)bytes;
            while (Mathf.Round(number / (float)1024) >= 1)
            {
                number = (float)number / (float)1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }  
    }  
    #endregion

    private static string _version = "0.0.2b";
    private void DrawNetwork() {
        GUILayout.Label($"Kittenji's Sprite Sheet Generator v{_version}", style_CenterLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Twitter", "This is my main twitter account, I might post some stuff there sometimes.")))
            Application.OpenURL("https://twitter.com/Kitt3nji");

        if (GUILayout.Button(new GUIContent("Twitch", "I also do live streams. I'm not as active anymore but might stream again eventually.")))
            Application.OpenURL("https://twitch.tv/kittenji");

        if (GUILayout.Button(new GUIContent("GitHub", "This is my personal GitHub account.")))
            Application.OpenURL("https://github.com/ChrisFeline");

        if (GUILayout.Button(new GUIContent("Reddit", "hmm... umm... yes, I have reddit.")))
            Application.OpenURL("https://reddit.com/user/Kittenji");

        EditorGUILayout.EndHorizontal();
    }
}
