#if TOOLS
using Godot;
using ImGuiNET;


namespace ImGuiGodot;

[Tool]
public partial class ImGuiEditorPlugin : EditorPlugin {

	#region VARIABLES

	private ImGuiLayer _imguiLayerControl;
	private Callable _onImGuiLayoutCallback;

	#endregion


	#region EditorPlugin override

	public override void _Ready() {
		// Initialization of the plugin goes here.
		Init();
	}

	public override void _EnterTree() {
		if ( !Engine.IsEditorHint() ) {
			AddAutoloadSingleton( "ImGuiLayer", "res://addons/imgui-godot/ImGuiLayer.tscn" );
		}
	}

	public override void _ExitTree() {

		if ( !Engine.IsEditorHint() ) {
			RemoveAutoloadSingleton( "ImGuiLayer" );
		}
		else {
			DeInit();

			ImGuiLayer.Instance = null;
		}
	}

	public override void _Process( double delta ) {
		base._Process( delta );
		UpdateOverlays();
	}

	public override bool _HasMainScreen() => false;

	public override void _MakeVisible( bool visible ) {
		// if ( _imguiLayerControl != null ) {
		// 	_imguiLayerControl.Visible = visible;
		// }
	}

	public override string _GetPluginName() => "ImGui";

	public override Texture2D _GetPluginIcon() {
		return EditorInterface.Singleton.GetBaseControl().GetThemeIcon( "ResourcePreloader", "EditorIcons" );
	}


	#endregion


	private void Init() {
		_imguiLayerControl = new ();
		_imguiLayerControl.Layer = 128;
		_imguiLayerControl.ProcessMode = ProcessModeEnum.Always;
		_imguiLayerControl.Visible = true;
		_onImGuiLayoutCallback = new Callable( this, MethodName.OnImGuiLayout );

		SetupWindow();

		ImGuiLayer.Connect( _onImGuiLayoutCallback );
	}

	private void DeInit() {
		// RemoveToolMenuItem( "Toggle ImGui" );

		if ( _imguiLayerControl is not null ) {
			ImGuiLayer.Disconnect( _onImGuiLayoutCallback );
			EditorInterface.Singleton.GetEditorMainScreen().RemoveChild( _imguiLayerControl );
			_imguiLayerControl.Free();
			_imguiLayerControl = null;
		}
	}

	private void SetupWindow() {
		if ( _imguiLayerControl is not null ) {
			EditorInterface.Singleton.GetEditorMainScreen().AddChild( _imguiLayerControl );
		}

		// _MakeVisible( false );
	}

	private void OnImGuiLayout() {
        // your code here
        ImGui.ShowDemoWindow();
	}


	private void ToggleImGuiLayer() {
		if ( _imguiLayerControl is not null ) {
			_imguiLayerControl.Visible = !_imguiLayerControl.Visible;
		}
	}

}
#endif
