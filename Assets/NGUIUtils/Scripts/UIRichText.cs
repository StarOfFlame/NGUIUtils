// #define SHOW_HIDDEN_OBJECTS

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

// http://wow.gamepedia.com/UI_escape_sequences
[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI RichText")]
[RequireComponent(typeof(UILabel))]
public class UIRichText : UIWidget
{
    class RichTextNode 
    {
        public string Text;
        public string Color;
        public string Link;
        public string Texture;
        
        public List<KeyValuePair<GameObject, Vector3>> Children = new List<KeyValuePair<GameObject, Vector3>>(); 
        public void OnClick(GameObject go)
        {
            go.transform.parent.SendMessage("OnRichTextClick", Link, SendMessageOptions.DontRequireReceiver);
        }
    }

    private UILabel Label;
    private Vector2 Postion;
    private float CurLineHight;
    private int LayoutWidth;
    private List<RichTextNode> CurRichText = new List<RichTextNode>();

    [HideInInspector][SerializeField] string mText = "";
    public string Text
    {
        get
        {
            return mText;
        }
        set
        {
            if (mText == value) 
                return;

            mText = value;
            LayoutText(mText);
        }
    }
    public bool AutoHight;

    protected override void OnStart()
    {
        Label = this.GetComponent<UILabel>();
        if (Label == null)
            throw new ArgumentNullException("UILabel not exist!");

        Label.enabled = false;
        UpdateText();
    }

    protected virtual void OnUpdate() 
    { 
        base.OnUpdate();
        UpdateText();
    }

    void UpdateText()
    {
        if (this.width == this.LayoutWidth)
            return;
        
        this.LayoutWidth = this.width;
        Label.width = this.width;
        Label.height = this.height;
        LayoutText(this.Text);
    }

    void CenterLayout()
    {
        if (AutoHight)
        {
            this.height = (int)Mathf.Round(Mathf.Abs(Postion.y) + CurLineHight);
            Label.height = this.height;
        }
        
        var offset = new Vector3(-this.LayoutWidth / 2f, this.height / 2f, 0);
        foreach (var node in CurRichText)
        {
            foreach (var child in node.Children)
            {
                child.Key.transform.localPosition = child.Value + offset;
            }
        }
    }
    
    void LayoutText(string msg)
    {
        if (CurRichText != null)
        {
            ClearText(CurRichText);
            CurRichText = null;
        }

        if (string.IsNullOrEmpty(msg))
            return;

		Postion = Vector2.zero;
		CurLineHight = 0;

        CurRichText = ParseText(msg);
        foreach (var node in CurRichText)
        {
            if (node.Texture != null)
            {
                var go = AddTexture(node);
                node.Children.Add(new KeyValuePair<GameObject, Vector3>(go, go.transform.localPosition));
            }
            else
            {
                var start = 0;
                while (start < node.Text.Length)
                {
                    bool newLine;
                    var count = FileLine(node.Text, start, this.LayoutWidth - Postion.x, out newLine);
                    if (count > 0)
                    {
                        var go = AddText(node, start, count);
                        node.Children.Add(new KeyValuePair<GameObject, Vector3>(go, go.transform.localPosition));
                        
                        start += count;
                    }
                    
                    if (newLine)
                        Newline();
                }
            }
        }

        CenterLayout();
    }

    void ClearText(List<RichTextNode> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var child in node.Children)
            {
                child.Key.SetActive(false);
				NGUITools.Destroy(child.Key);
            }
        }
        nodes.Clear();
    }

    void Newline()
    {
        Postion.x = 0;
        Postion.y -= CurLineHight;
        CurLineHight = 0;
    }

    Vector2 CalculationFontSize(string msg)
    {
        Label.text = msg;
        return Label.printedSize;
    } 

    int FileLine(string text, int start, float leftWidth, out bool newLine)
    {
        newLine = false;

        for (var i = start; i < text.Length; ++i)
        {
            var ch = text[i];
            if (ch == '\n')
            {
                newLine = true;
                return i - start;
            }

            var fontSize = CalculationFontSize(ch.ToString());
            if (fontSize.x > leftWidth)
            {
                newLine = true;
                return i - start;
            }
            
            leftWidth -= fontSize.x;
        }
        return text.Length - start;
    }

    GameObject AddTexture(RichTextNode node)
    {
        if (node.Texture == null)
            throw new ArgumentException("node");

        var assert = Resources.Load(node.Texture);
        var go = GameObject.Instantiate(assert) as GameObject;
#if UNITY_EDITOR
#if SHOW_HIDDEN_OBJECTS
        go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
#else
        go.hideFlags = HideFlags.HideAndDontSave;
#endif
#endif

        var sprite = go.GetComponent<UISprite>();
        if (Postion.x + sprite.width > this.LayoutWidth)
            Newline();

        go.transform.SetParent(this.transform, false);
        go.transform.localPosition = new Vector3(Postion.x + sprite.width / 2, Postion.y - sprite.height / 2, 0);
        
        Postion.x += sprite.width;
        CurLineHight = Mathf.Max(CurLineHight, sprite.height);

        return go;
    }

    GameObject AddText(RichTextNode node, int start, int count)
    {
        var text = node.Text.Substring(start, count);
        var color = node.Color;

        GameObject go = new GameObject(text);
#if UNITY_EDITOR
#if SHOW_HIDDEN_OBJECTS
        go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
#else
        go.hideFlags = HideFlags.HideAndDontSave;
#endif
#endif

        var label = go.AddComponent<UILabel>();
        label.bitmapFont = Label.bitmapFont;
        label.trueTypeFont = Label.trueTypeFont;
        label.fontSize = Label.fontSize;
        label.fontStyle = Label.fontStyle;
        label.alignment = Label.alignment;
        label.width = 0;
        label.height = 0;
        label.color = new Color(
            int.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(color.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f);

        label.text = text;
        label.MakePixelPerfect();
        
        var size = label.printedSize;
        label.transform.SetParent(this.transform, false);
        label.transform.localPosition = new Vector3(Postion.x + size.x / 2, Postion.y - size.y / 2, 0);

        if (node.Link != null)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, size.y);

            UIEventListener.Get(go).onClick = node.OnClick;
        }

        Postion.x += size.x;
        CurLineHight = Mathf.Max(CurLineHight, size.y);
        return go;
    }

    void AddText(List<RichTextNode> nodes, string text, string color, string link)
    {
        if (nodes.Count > 0)
        {
            var node = nodes[nodes.Count - 1];
            if (node.Color == color && node.Link == link)
            {
                node.Text += text;
                return;
            }
        }

        var newNode = new RichTextNode();
        newNode.Text = text;
        newNode.Color = color;
        newNode.Link = link;
        nodes.Add(newNode);
    }

    List<RichTextNode> ParseText(string text)
    {
        var nodes = new List<RichTextNode>();
        string originalColor = "FFFFFFFF";
        string currentColor = originalColor;
        string currentLink = null;

        var start = 0;
        while (true)
        {
            var index = text.IndexOf("|", start);
            if (index == -1)
                index = text.Length;

            if (index > start)
                AddText(nodes, text.Substring(start, index - start), currentColor, currentLink);

            if (index == text.Length)
                break;

            index++;
            var cur = text[index];
            switch (cur)
            {
                case '|':
                    AddText(nodes, "|", currentColor, currentLink);
                    start = index + 1;
                    break;

                case 'n':
                    AddText(nodes, "\n", currentColor, currentLink);
                    start = index + 1;
                    break;

                case 'c':
                    index++;
                    currentColor = text.Substring(index, 8);
                    start = index + 8;
                    break;

                case 'r':
                    currentColor = originalColor;
                    start = index + 1;
                    break;

                case 'T':
                {
                    index++;
                    var nextIndex = text.IndexOf("|t", index);

                    var node = new RichTextNode();
                    node.Texture = text.Substring(index, nextIndex - index);
                    nodes.Add(node);
                    
                    start = nextIndex + 2;
                    break;
                }

                case 'H':
                {
                    index++;
                    var nextIndex = text.IndexOf("|h", index);
                    currentLink = text.Substring(index, nextIndex - index);
                    start = nextIndex + 2;
                    break;
                }

                case 'h':
                    if (currentLink == null)
                        throw new FormatException("currentLink == null");

                    currentLink = null;
                    start = index + 1;
                    break;
            }
        }
        return nodes;
    }
}


