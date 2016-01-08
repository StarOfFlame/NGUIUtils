// #define SHOW_HIDDEN_OBJECTS

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

// http://wow.gamepedia.com/UI_escape_sequences
[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI RichText")]
public class UIRichText : UIWidget
{
	[HideInInspector][SerializeField] Font mTrueTypeFont;
	[HideInInspector][SerializeField] UIFont mFont;
	[HideInInspector][SerializeField] int mFontSize = 16;
	[HideInInspector][SerializeField] FontStyle mFontStyle = FontStyle.Normal;
    [HideInInspector][SerializeField] string mText = "";
	[HideInInspector][SerializeField] bool mApplyGradient = false;
	[HideInInspector][SerializeField] Color mGradientTop = Color.white;
	[HideInInspector][SerializeField] Color mGradientBottom = new Color(0.7f, 0.7f, 0.7f);
	[HideInInspector][SerializeField] UILabel.Effect mEffectStyle = UILabel.Effect.None;
	[HideInInspector][SerializeField] Color mEffectColor = Color.black;
	[HideInInspector][SerializeField] Vector2 mEffectDistance = Vector2.one;
	[HideInInspector][SerializeField] int mSpacingX = 0;
	[HideInInspector][SerializeField] int mSpacingY = 0;
	[HideInInspector][SerializeField] bool mUseFloatSpacing = false;
	[HideInInspector][SerializeField] float mFloatSpacingX = 0;
	[HideInInspector][SerializeField] float mFloatSpacingY = 0;

	[HideInInspector][SerializeField] bool mResizeHight = false;

	public string text
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

    private UILabel mLabel;
    private Vector2 mPostion;
    private float mCurLineHight;
    private int mLayoutWidth;
    private List<RichTextNode> mCurRichText = new List<RichTextNode>();

#if UNITY_EDITOR
	protected override void OnValidate ()
	{
		base.OnValidate();
		if (NGUITools.GetActive (this)) 
		{
			this.mLayoutWidth = 0;
		}
	}
#endif

	protected override void OnStart()
    {
		base.OnStart();
        UpdateText();
    }

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateText();
	}

    void UpdateText()
    {
		if (this.mLayoutWidth == this.width)
			return;

		if (this.mLabel != null) 
		{
			NGUITools.Destroy(this.mLabel.gameObject);
			this.mLabel = null;
		}

		this.mLayoutWidth = this.width;
		LayoutText(this.text);
    }

    void CenterLayout()
    {
		if (mResizeHight)
        {
			var height = this.height;
			this.height = (int)Mathf.Round(Mathf.Abs(mPostion.y) + mCurLineHight);

			var position = this.transform.localPosition;
			position.y -= (this.height - height) / 2f;
			this.transform.localPosition = position;
        }
        
		var offset = new Vector3(-this.mLayoutWidth / 2f, this.height / 2f, 0);
        foreach (var node in mCurRichText)
        {
            foreach (var child in node.Children)
            {
                child.Key.transform.localPosition = child.Value + offset;
            }
        }
    }
    
    void LayoutText(string msg)
    {
        if (mCurRichText != null)
        {
            ClearText(mCurRichText);
            mCurRichText = null;
        }

        if (string.IsNullOrEmpty(msg))
            return;

		mPostion = Vector2.zero;
		mCurLineHight = 0;
		
		mCurRichText = ParseText(msg);
		foreach (var node in mCurRichText)
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
					var count = FileLine(node.Text, start, this.mLayoutWidth - mPostion.x, out newLine);
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
		mPostion.x = 0;
		mPostion.y -= mCurLineHight;
        mCurLineHight = 0;
    }

    Vector2 CalculationFontSize(string msg)
    {
		var label = GetLabel ();
		label.text = msg;
		return label.printedSize;
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
			if (fontSize.x > this.width)
				throw new InvalidCastException("fontSize.x > this.width");

            if (fontSize.x > leftWidth)
            {
                newLine = true;
                return i - start;
            }
            
            leftWidth -= fontSize.x;
        }
        return text.Length - start;
    }

	void HideGameObject(GameObject go)
	{
#if UNITY_EDITOR
#if SHOW_HIDDEN_OBJECTS
		go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
#else
		go.hideFlags = HideFlags.HideAndDontSave;
#endif
#endif
	}

	UILabel GetLabel()
	{
		if (mLabel == null) 
		{
			var go = new GameObject();
			go.layer = this.gameObject.layer;
			go.SetActive(false);
			go.transform.SetParent(this.transform, false);
			HideGameObject(go);
			
			var label = go.AddComponent<UILabel>();
			label.trueTypeFont = this.mTrueTypeFont;
			label.bitmapFont = this.mFont;
			label.fontSize = this.mFontSize;
			label.fontStyle = this.mFontStyle;
			label.applyGradient = this.mApplyGradient;
			label.gradientTop = this.mGradientTop;
			label.gradientBottom = this.mGradientBottom;
			label.effectStyle = this.mEffectStyle;
			label.effectColor = this.mEffectColor;
			label.effectDistance = this.mEffectDistance;
			label.useFloatSpacing = this.mUseFloatSpacing;
			label.spacingX = this.mSpacingX;
			label.spacingY = this.mSpacingY;
			label.floatSpacingX = this.mFloatSpacingX;
			label.floatSpacingY = this.mFloatSpacingY;

			label.overflowMethod = UILabel.Overflow.ResizeFreely;
			label.width = 0;
			label.height = 0;
			label.depth = this.depth;

			mLabel = label;
		}
		return mLabel;
	}

    GameObject AddTexture(RichTextNode node)
    {
        if (node.Texture == null)
            throw new ArgumentException("node");

        var assert = Resources.Load(node.Texture);
        var go = GameObject.Instantiate(assert) as GameObject;
		HideGameObject(go);

        var sprite = go.GetComponent<UISprite>();
		sprite.depth = mLabel.depth;
		
        if (mPostion.x + sprite.width > this.mLayoutWidth)
            Newline();

        go.transform.SetParent(this.transform, false);
        go.transform.localPosition = new Vector3(mPostion.x + sprite.width / 2, mPostion.y - sprite.height / 2, 0);
        
        mPostion.x += sprite.width;
        mCurLineHight = Mathf.Max(mCurLineHight, sprite.height);

        return go;
    }

    GameObject AddText(RichTextNode node, int start, int count)
    {
        var text = node.Text.Substring(start, count);
        var color = node.Color;

		var go = GameObject.Instantiate(mLabel.gameObject) as GameObject;
		go.name = text;
		go.SetActive (true);
		HideGameObject(go);

		var label = go.GetComponent<UILabel>();
        label.width = 0;
        label.height = 0;
        label.color = new Color(
            int.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(color.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f);

        label.text = text;

        var size = label.printedSize;
        label.transform.SetParent(this.transform, false);
        label.transform.localPosition = new Vector3(mPostion.x + size.x / 2, mPostion.y - size.y / 2, 0);

        if (node.Link != null)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, size.y);

            UIEventListener.Get(go).onClick = node.OnClick;
        }

        mPostion.x += size.x;
        mCurLineHight = Mathf.Max(mCurLineHight, size.y);
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


