using Godot;

public partial class HueRing : ColorRect
{
    [Export]
    public float InnerRadius = 0.35f;

    [Export]
    public float OuterRadius = 0.5f;

    [Signal]
    public delegate void HueChangedEventHandler(float hue);

    float _hue = 0f;
    bool _dragging = false;
    Control overlay = new Control();

    public override void _Ready()
    {
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(overlay);
        overlay.Draw += () =>
        {
            float midRadius = (InnerRadius + OuterRadius) * 0.5f * Size.X;
            float angle = (_hue - 0.5f) * Mathf.Tau;
            Vector2 center = Size / 2f;
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * midRadius;

            overlay.DrawCircle(pos, 6f, Colors.White);
            overlay.DrawArc(pos, 6f, 0f, Mathf.Tau, 24, Colors.Black, 1.5f);
        };
    }

    public float Hue
    {
        get => _hue;
        set
        {
            _hue = Mathf.PosMod(value, 1f);
            EmitSignal(SignalName.HueChanged, _hue);
            overlay.QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
            if (_dragging)
                TrySetHue(mb.Position);
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            TrySetHue(mm.Position);
        }
    }

    void TrySetHue(Vector2 pos)
    {
        Vector2 center = Size / 2f;
        Vector2 offset = pos - center;
        float dist = offset.Length() / center.X;

        if (dist >= InnerRadius * 2f && dist <= OuterRadius * 2f)
            Hue = (Mathf.Atan2(offset.Y, offset.X) / Mathf.Tau) + 0.5f;
    }
}
