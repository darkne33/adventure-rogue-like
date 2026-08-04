using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ExpBarUpgradeMarquee : MaskableGraphic
{
    private const int GlyphColumns = 5;
    private const int GlyphRows = 7;
    private const float LetterGapInPixels = 1.5f;
    private const string EmptyGlyph = "00000000000000000000000000000000000";
    private const string UnknownGlyph = "01110100010000100010001000000000100";

    private static readonly Color32[] DefaultRainbowColors =
    {
        new(250, 48, 72, 255),
        new(255, 132, 32, 255),
        new(255, 220, 48, 255),
        new(48, 205, 92, 255),
        new(30, 186, 232, 255),
        new(55, 103, 245, 255),
        new(174, 67, 232, 255)
    };

    [Header("Rainbow")]
    [SerializeField] private Color[] _rainbowColors =
    {
        new Color32(250, 48, 72, 255),
        new Color32(255, 132, 32, 255),
        new Color32(255, 220, 48, 255),
        new Color32(48, 205, 92, 255),
        new Color32(30, 186, 232, 255),
        new Color32(55, 103, 245, 255),
        new Color32(174, 67, 232, 255)
    };
    [SerializeField, Min(1)] private int _visibleRainbowSegments = 14;
    [SerializeField, Min(0f)] private float _segmentsPerSecond = 1.35f;

    [Header("Text ticker")]
    [SerializeField] private string _tickerText = "EXP";
    [SerializeField, Range(0.2f, 0.8f)] private float _textHeightRatio = 0.5f;
    [SerializeField, Min(0f)] private float _labelGapHeightRatio = 1.1f;
    [SerializeField, Range(0.5f, 1f)] private float _pixelFill = 0.82f;
    [SerializeField, Range(0f, 1.5f)] private float _shadowOffsetInPixels = 0.65f;
    [SerializeField] private Color _textColor = Color.white;
    [SerializeField] private Color _shadowColor = new(0.08f, 0.03f, 0.12f, 0.72f);
    [SerializeField] private Color _borderColor = new(0.08f, 0.03f, 0.12f, 0.45f);

    private float _rainbowTravel;
    private float _textTravel;

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            if (gameObject.activeSelf == false)
                gameObject.SetActive(true);

            ResetAnimation();
            return;
        }

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void SetText(string text)
    {
        string newText = text ?? string.Empty;
        if (_tickerText == newText)
            return;

        _tickerText = newText;
        _textTravel = 0f;

        if (IsActive())
            SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetAnimation();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        _visibleRainbowSegments = Mathf.Max(1, _visibleRainbowSegments);
        _segmentsPerSecond = Mathf.Max(0f, _segmentsPerSecond);
        _labelGapHeightRatio = Mathf.Max(0f, _labelGapHeightRatio);
        SetVerticesDirty();
    }

    private void Update()
    {
        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float segmentWidth = rect.width / _visibleRainbowSegments;
        float deltaTime = Time.unscaledDeltaTime;
        float pixelDistance = segmentWidth * _segmentsPerSecond * deltaTime;

        _rainbowTravel = Mathf.Repeat(
            _rainbowTravel - _segmentsPerSecond * deltaTime,
            GetRainbowColorCount());
        _textTravel = Mathf.Repeat(
            _textTravel - pixelDistance,
            GetLabelAdvance(rect.height));

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        AddRainbow(vertexHelper, rect);
        AddTextTicker(vertexHelper, rect);
        AddBorders(vertexHelper, rect);
    }

    private void ResetAnimation()
    {
        _rainbowTravel = 0f;
        _textTravel = 0f;

        if (IsActive())
            SetVerticesDirty();
    }

    private void AddRainbow(VertexHelper vertexHelper, Rect clipRect)
    {
        float segmentWidth = clipRect.width / _visibleRainbowSegments;
        int firstColorIndex = Mathf.FloorToInt(_rainbowTravel);
        float segmentPhase = _rainbowTravel - firstColorIndex;
        float x = clipRect.xMin - segmentPhase * segmentWidth;

        while (x < clipRect.xMax)
        {
            Color segmentColor = GetRainbowColor(firstColorIndex);
            AddClippedQuad(
                vertexHelper,
                Rect.MinMaxRect(x, clipRect.yMin, x + segmentWidth, clipRect.yMax),
                clipRect,
                MultiplyColor(segmentColor, color));

            x += segmentWidth;
            firstColorIndex++;
        }
    }

    private void AddTextTicker(VertexHelper vertexHelper, Rect clipRect)
    {
        if (string.IsNullOrEmpty(_tickerText))
            return;

        float pixelSize = clipRect.height * _textHeightRatio / GlyphRows;
        float labelAdvance = GetLabelAdvance(clipRect.height);
        float labelWidth = GetLabelWidth(pixelSize);
        float labelBottom = clipRect.center.y - GlyphRows * pixelSize * 0.5f;
        float x = clipRect.xMin - _textTravel;
        float shadowOffset = pixelSize * _shadowOffsetInPixels;

        while (x < clipRect.xMax)
        {
            if (x + labelWidth > clipRect.xMin)
            {
                AddTextLabel(vertexHelper, clipRect, x + shadowOffset, labelBottom - shadowOffset,
                    pixelSize, MultiplyColor(_shadowColor, color));
                AddTextLabel(vertexHelper, clipRect, x, labelBottom, pixelSize,
                    MultiplyColor(_textColor, color));
            }

            x += labelAdvance;
        }
    }

    private void AddTextLabel(VertexHelper vertexHelper, Rect clipRect, float x, float y,
        float pixelSize, Color labelColor)
    {
        float letterAdvance = (GlyphColumns + LetterGapInPixels) * pixelSize;
        float glyphWidth = GlyphColumns * pixelSize;
        int firstVisibleGlyph = Mathf.Max(
            0,
            Mathf.FloorToInt((clipRect.xMin - x - glyphWidth) / letterAdvance) + 1);
        int lastVisibleGlyph = Mathf.Min(
            _tickerText.Length - 1,
            Mathf.CeilToInt((clipRect.xMax - x) / letterAdvance) - 1);

        for (int i = firstVisibleGlyph; i <= lastVisibleGlyph; i++)
        {
            string glyph = GetGlyph(_tickerText[i]);
            AddGlyph(vertexHelper, clipRect, glyph, x + letterAdvance * i, y, pixelSize, labelColor);
        }
    }

    private void AddGlyph(VertexHelper vertexHelper, Rect clipRect, string glyph, float x, float y,
        float pixelSize, Color glyphColor)
    {
        float inset = pixelSize * (1f - _pixelFill) * 0.5f;
        float filledPixelSize = pixelSize - inset * 2f;

        for (int row = 0; row < GlyphRows; row++)
        {
            float pixelY = y + (GlyphRows - row - 1) * pixelSize + inset;

            for (int column = 0; column < GlyphColumns; column++)
            {
                if (glyph[row * GlyphColumns + column] != '1')
                    continue;

                float pixelX = x + column * pixelSize + inset;
                AddClippedQuad(
                    vertexHelper,
                    new Rect(pixelX, pixelY, filledPixelSize, filledPixelSize),
                    clipRect,
                    glyphColor);
            }
        }
    }

    private void AddBorders(VertexHelper vertexHelper, Rect rect)
    {
        float borderHeight = Mathf.Max(1f, rect.height * 0.045f);
        Color border = MultiplyColor(_borderColor, color);

        AddClippedQuad(vertexHelper,
            Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, rect.yMin + borderHeight), rect, border);
        AddClippedQuad(vertexHelper,
            Rect.MinMaxRect(rect.xMin, rect.yMax - borderHeight, rect.xMax, rect.yMax), rect, border);
    }

    private float GetLabelAdvance(float rectHeight)
    {
        float pixelSize = rectHeight * _textHeightRatio / GlyphRows;
        return Mathf.Max(1f, GetLabelWidth(pixelSize) + rectHeight * _labelGapHeightRatio);
    }

    private float GetLabelWidth(float pixelSize)
    {
        if (string.IsNullOrEmpty(_tickerText))
            return 0f;

        return (GlyphColumns + (GlyphColumns + LetterGapInPixels) * (_tickerText.Length - 1)) * pixelSize;
    }

    private static string GetGlyph(char character)
    {
        return char.ToUpperInvariant(character) switch
        {
            ' ' => EmptyGlyph,
            'A' or 'А' => "01110" + "10001" + "10001" + "11111" + "10001" + "10001" + "10001",
            'B' or 'В' => "11110" + "10001" + "10001" + "11110" + "10001" + "10001" + "11110",
            'C' or 'С' => "01111" + "10000" + "10000" + "10000" + "10000" + "10000" + "01111",
            'D' => "11110" + "10001" + "10001" + "10001" + "10001" + "10001" + "11110",
            'E' or 'Е' => "11111" + "10000" + "10000" + "11110" + "10000" + "10000" + "11111",
            'F' => "11111" + "10000" + "10000" + "11110" + "10000" + "10000" + "10000",
            'G' => "01111" + "10000" + "10000" + "10111" + "10001" + "10001" + "01111",
            'H' or 'Н' => "10001" + "10001" + "10001" + "11111" + "10001" + "10001" + "10001",
            'I' => "11111" + "00100" + "00100" + "00100" + "00100" + "00100" + "11111",
            'J' => "00111" + "00010" + "00010" + "00010" + "00010" + "10010" + "01100",
            'K' or 'К' => "10001" + "10010" + "10100" + "11000" + "10100" + "10010" + "10001",
            'L' => "10000" + "10000" + "10000" + "10000" + "10000" + "10000" + "11111",
            'M' or 'М' => "10001" + "11011" + "10101" + "10101" + "10001" + "10001" + "10001",
            'N' => "10001" + "11001" + "10101" + "10011" + "10001" + "10001" + "10001",
            'O' or 'О' => "01110" + "10001" + "10001" + "10001" + "10001" + "10001" + "01110",
            'P' or 'Р' => "11110" + "10001" + "10001" + "11110" + "10000" + "10000" + "10000",
            'Q' => "01110" + "10001" + "10001" + "10001" + "10101" + "10010" + "01101",
            'R' => "11110" + "10001" + "10001" + "11110" + "10100" + "10010" + "10001",
            'S' => "01111" + "10000" + "10000" + "01110" + "00001" + "00001" + "11110",
            'T' or 'Т' => "11111" + "00100" + "00100" + "00100" + "00100" + "00100" + "00100",
            'U' => "10001" + "10001" + "10001" + "10001" + "10001" + "10001" + "01110",
            'V' => "10001" + "10001" + "10001" + "10001" + "10001" + "01010" + "00100",
            'W' => "10001" + "10001" + "10001" + "10001" + "10101" + "11011" + "10001",
            'X' or 'Х' => "10001" + "01010" + "00100" + "00100" + "00100" + "01010" + "10001",
            'Y' => "10001" + "10001" + "01010" + "00100" + "00100" + "00100" + "00100",
            'Z' => "11111" + "00001" + "00010" + "00100" + "01000" + "10000" + "11111",
            'Б' => "11111" + "10000" + "10000" + "11110" + "10001" + "10001" + "11110",
            'Г' => "11111" + "10000" + "10000" + "10000" + "10000" + "10000" + "10000",
            'Д' => "00110" + "01010" + "01010" + "10010" + "10010" + "11111" + "10001",
            'Ё' => "01010" + "00000" + "11111" + "10000" + "11110" + "10000" + "11111",
            'Ж' => "10101" + "10101" + "01110" + "00100" + "01110" + "10101" + "10101",
            'З' => "11110" + "00001" + "00001" + "01110" + "00001" + "00001" + "11110",
            'И' => "10001" + "10011" + "10101" + "10101" + "11001" + "10001" + "10001",
            'Й' => "01010" + "00100" + "10001" + "10011" + "10101" + "11001" + "10001",
            'Л' => "00111" + "01001" + "10001" + "10001" + "10001" + "10001" + "10001",
            'П' => "11111" + "10001" + "10001" + "10001" + "10001" + "10001" + "10001",
            'У' => "10001" + "10001" + "01010" + "00100" + "00100" + "01000" + "10000",
            'Ф' => "00100" + "01110" + "10101" + "10101" + "01110" + "00100" + "00100",
            'Ц' => "10010" + "10010" + "10010" + "10010" + "10010" + "11111" + "00001",
            'Ч' => "10001" + "10001" + "10001" + "01111" + "00001" + "00001" + "00001",
            'Ш' => "10101" + "10101" + "10101" + "10101" + "10101" + "10101" + "11111",
            'Щ' => "10101" + "10101" + "10101" + "10101" + "10101" + "11111" + "00001",
            'Ъ' => "11000" + "01000" + "01110" + "01001" + "01001" + "01001" + "01110",
            'Ы' => "10001" + "10001" + "11101" + "10011" + "10011" + "10011" + "11101",
            'Ь' => "10000" + "10000" + "11110" + "10001" + "10001" + "10001" + "11110",
            'Э' => "11110" + "00001" + "00001" + "01111" + "00001" + "00001" + "11110",
            'Ю' => "10110" + "11001" + "11001" + "11001" + "11001" + "11001" + "10110",
            'Я' => "01111" + "10001" + "10001" + "01111" + "00101" + "01001" + "10001",
            '0' => "01110" + "10001" + "10011" + "10101" + "11001" + "10001" + "01110",
            '1' => "00100" + "01100" + "00100" + "00100" + "00100" + "00100" + "11111",
            '2' => "01110" + "10001" + "00001" + "00010" + "00100" + "01000" + "11111",
            '3' => "11110" + "00001" + "00001" + "01110" + "00001" + "00001" + "11110",
            '4' => "00010" + "00110" + "01010" + "10010" + "11111" + "00010" + "00010",
            '5' => "11111" + "10000" + "10000" + "11110" + "00001" + "00001" + "11110",
            '6' => "01110" + "10000" + "10000" + "11110" + "10001" + "10001" + "01110",
            '7' => "11111" + "00001" + "00010" + "00100" + "01000" + "01000" + "01000",
            '8' => "01110" + "10001" + "10001" + "01110" + "10001" + "10001" + "01110",
            '9' => "01110" + "10001" + "10001" + "01111" + "00001" + "00001" + "01110",
            '-' => "00000" + "00000" + "00000" + "11111" + "00000" + "00000" + "00000",
            '+' => "00000" + "00100" + "00100" + "11111" + "00100" + "00100" + "00000",
            '!' => "00100" + "00100" + "00100" + "00100" + "00100" + "00000" + "00100",
            '?' => UnknownGlyph,
            '.' => "00000" + "00000" + "00000" + "00000" + "00000" + "00000" + "00100",
            '_' => "00000" + "00000" + "00000" + "00000" + "00000" + "00000" + "11111",
            '/' => "00001" + "00010" + "00010" + "00100" + "01000" + "01000" + "10000",
            _ => UnknownGlyph
        };
    }

    private int GetRainbowColorCount() =>
        _rainbowColors is { Length: > 0 } ? _rainbowColors.Length : DefaultRainbowColors.Length;

    private Color GetRainbowColor(int index)
    {
        int colorCount = GetRainbowColorCount();
        int wrappedIndex = ((index % colorCount) + colorCount) % colorCount;

        return _rainbowColors is { Length: > 0 }
            ? _rainbowColors[wrappedIndex]
            : DefaultRainbowColors[wrappedIndex];
    }

    private static Color MultiplyColor(Color first, Color second) =>
        new(first.r * second.r, first.g * second.g, first.b * second.b, first.a * second.a);

    private static void AddClippedQuad(VertexHelper vertexHelper, Rect sourceRect, Rect clipRect, Color color)
    {
        float xMin = Mathf.Max(sourceRect.xMin, clipRect.xMin);
        float xMax = Mathf.Min(sourceRect.xMax, clipRect.xMax);
        float yMin = Mathf.Max(sourceRect.yMin, clipRect.yMin);
        float yMax = Mathf.Min(sourceRect.yMax, clipRect.yMax);

        if (xMax <= xMin || yMax <= yMin)
            return;

        int startIndex = vertexHelper.currentVertCount;
        Color32 vertexColor = color;

        vertexHelper.AddVert(new Vector3(xMin, yMin), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector3(xMin, yMax), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector3(xMax, yMax), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector3(xMax, yMin), vertexColor, Vector2.zero);
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
