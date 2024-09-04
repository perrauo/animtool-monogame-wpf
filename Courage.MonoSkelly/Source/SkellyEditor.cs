using GeonBit.UI;
using GeonBit.UI.Entities;
using GeonBit.UI.Entities.TextValidators;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
using System.Windows.Input;
using Courage.MonoSkelly;
using MonoSkelly.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace MonoSkelly.Editor
{
	/// <summary>
	/// MonoSkelly editor software.
	/// </summary>
	public class SkellyEditor : Game
	{
		GraphicsDevice _graphicsDevice;
		//private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		// currently presented skeleton and camera
		Skeleton _skeleton;
		Camera _camera;

		// camera settings
		float _cameraDistance;
		Vector2 _cameraAngle;
		float _cameraOffsetY;

		// input helpers
		int _prevMouseWheel;
		Vector2 _prevMousePos;

		// should we debug draw bones and handles?
		bool _showBones = true;
		bool _showBoneHandles = true;
		bool _showBonesOutline = true;
		bool _showLights = true;

		// convert bone full path to index in bones list.
		Dictionary<string, int> _fullPathToBoneListIndex;

		// currently selected bone
		string _selectedBone;

		// field to change while dragging mouse
		TextInput _draggedInput;
		float _draggedTime;

		// floor model
		Model _floorModel;
		Texture2D _floorTexture;

		// saved files folder
		static string _savesFolder = "saves";

		// currently loaded file name
		string _currentFilename;

		// name to use for default pose without animation
		static string DefaultNoneAnimationName = "Default Pose";

		private AnimsPanel _animsPanel;

		private BonesAndTransformsPanel _bonesAndTransformsPanel;

		private MainWindow _mainWindow;

		private TimelineControl _timelineControl;

		private AnimationState _animation;

		private Dictionary<string, Model> _bodyParts;

		/// <summary>
		/// Create editor.
		/// </summary>
		public SkellyEditor(GraphicsDevice graphicsDevice)
		{
			_graphicsDevice = graphicsDevice;
			Content.RootDirectory = "Content";
			IsMouseVisible = false;
		}


		public void CallProtectedInitialize()
		{
			Initialize();

		}


		/// <summary>
		/// Initialize editor.
		/// </summary>
		protected override void Initialize()
		{
			_mainWindow = (MainWindow)Application.Current.MainWindow;
			_mainWindow.MouseWheel += OnMouseWheel;
			_mainWindow.NewMenuItem.Click += PressedTopMenu_New;
			_mainWindow.SaveMenuItem.Click += PressedTopMenu_Save;
			_mainWindow.SaveAsMenuItem.Click += PressedTopMenu_SaveAs;
			_mainWindow.LoadMenuItem.Click += PressedTopMenu_Load;
			_mainWindow.ExitMenuItem.Click += PressedTopMenu_Exit;

			_animsPanel = _mainWindow.FindChild<AnimsPanel>();
			_bonesAndTransformsPanel = _mainWindow.FindChild<BonesAndTransformsPanel>();

			_timelineControl = _mainWindow.FindChild<TimelineControl>();
			_timelineControl.OnPlaybackFrame += OnPlaybackFrame;
			_timelineControl.OnAnimationSelected += OnAnimationChanged;
			_timelineControl.OnKeyframeChanged += OnKeyframeChanged;

			// create skeleton and camera
			_skeleton = new Skeleton();
			_camera = new Camera(_graphicsDevice);
			ResetCamera();

			// make sure saves folder exist
			if(!System.IO.Directory.Exists(_savesFolder)) { System.IO.Directory.CreateDirectory(_savesFolder); }

			// call base init
			base.Initialize();
		}

		void OnAnimationChanged(string animation)
		{
			_animation = _skeleton.BeginAnimation(animation);
			_timelineControl.OnAnimationChanged(_animation);
		}

		void OnKeyframeChanged(AnimationStep step, float timestamp)
		{
			if(_animation)
			{
				var anim = _skeleton.GetAnimation(_animation.Name);
				if(anim != null)
				{
					anim.SetStepAtTimestamp(step, timestamp);
					// Start animation anew to account for keyframe changes
					_animation = _skeleton.BeginAnimation(_animation.Name);
					_animation.Update(_timelineControl.TimeStamp);
				}
			}
		}

		void OnPlaybackFrame(float delta)
		{
			_animation.Update(delta);
		}


		/// <summary>
		/// Load a project.
		/// </summary>
		void LoadProject(string file)
		{
			// load skeleton
			_skeleton = new Skeleton();
			_skeleton.LoadFrom(new Sini.IniFile(System.IO.Path.Combine(_savesFolder, file)));
			_timelineControl.LoadProject(_skeleton);
			// Load in the keyframes

			// update parts list
			UpdatePartsList();

			// set filename
			UpdateFilename(file);

		}

		/// <summary>
		/// Create empty skeleton.
		/// </summary>
		void CreateEmptySkeleton()
		{
			// create skeleton
			_skeleton = new Skeleton();
			_skeleton.AddBone("root");

			// update parts list
			UpdatePartsList();

			// reset filename
			UpdateFilename(null);
		}

		/// <summary>
		/// Create a default human skeleton.
		/// </summary>
		void CreateDefaultSkeleton()
		{
			// create skeleton
			_skeleton = new Skeleton();
			_skeleton.AddBone("root");

			// add starting bones
			var torsoHeight = 3f;
			var torsoOffset = 8f + torsoHeight;
			_skeleton.AddBone("root/torso", (new Vector3(0, torsoOffset, 0)));
			_skeleton.AddBone("root/torso/upper", (new Vector3(0, 0.5f, 0)));
			_skeleton.AddBone("root/torso/lower", (new Vector3(0, -torsoHeight / 2, 0)));
			_skeleton.SetBonePreviewModel("root/torso/upper", new Vector3(0, torsoHeight / 2 - 0.25f, 0), new Vector3(1.75f, torsoHeight / 2, 1f));
			_skeleton.SetBonePreviewModel("root/torso/lower", new Vector3(0, 0, 0), new Vector3(1.75f, torsoHeight / 2 - 0.25f, 1f));

			var thighLength = 2f;
			_skeleton.AddBone("root/torso/lower/legs", (new Vector3(0, -torsoHeight / 2, 0)));
			_skeleton.AddBone("root/torso/lower/legs/left", (new Vector3(-1f, 0, 0)));
			_skeleton.AddBone("root/torso/lower/legs/right", (new Vector3(1f, 0, 0)));
			_skeleton.SetBonePreviewModel("root/torso/lower/legs/left", new Vector3(0f, -thighLength, 0f), new Vector3(0.85f, thighLength, 1f));
			_skeleton.SetBonePreviewModel("root/torso/lower/legs/right", new Vector3(0f, -thighLength, 0f), new Vector3(0.85f, thighLength, 1f));

			var lowerLegLength = 2f;
			_skeleton.AddBone("root/torso/lower/legs/left/lower", (new Vector3(0, -thighLength * 2, 0)));
			_skeleton.AddBone("root/torso/lower/legs/right/lower", (new Vector3(0, -thighLength * 2, 0)));
			_skeleton.SetBonePreviewModel("root/torso/lower/legs/left/lower", new Vector3(0f, -lowerLegLength, 0f), new Vector3(0.85f, lowerLegLength, 1f));
			_skeleton.SetBonePreviewModel("root/torso/lower/legs/right/lower", new Vector3(0f, -lowerLegLength, 0f), new Vector3(0.85f, lowerLegLength, 1f));

			_skeleton.AddBone("root/torso/lower/legs/left/lower/foot", (new Vector3(0, -lowerLegLength * 2, 0)));
			_skeleton.AddBone("root/torso/lower/legs/right/lower/foot", (new Vector3(0, -lowerLegLength * 2, 0)));
			_skeleton.SetBonePreviewModel("root/torso/lower/legs/left/lower/foot", new Vector3(0f, 0.5f, 1f), new Vector3(0.85f, 0.5f, 1.75f));
			_skeleton.SetBonePreviewModel("root/torso/lower/legs/right/lower/foot", new Vector3(0f, 0.5f, 1f), new Vector3(0.85f, 0.5f, 1.75f));

			var armTopLength = 1.5f;
			_skeleton.AddBone("root/torso/upper/arms", (new Vector3(0, torsoHeight - 0.5f, 0)));
			_skeleton.AddBone("root/torso/upper/arms/left", (new Vector3(-2.5f, 0f, 0f)));
			_skeleton.AddBone("root/torso/upper/arms/right", (new Vector3(2.5f, 0f, 0f)));
			_skeleton.SetBonePreviewModel("root/torso/upper/arms/left", new Vector3(0f, -armTopLength, 0f), new Vector3(0.65f, armTopLength, 1f));
			_skeleton.SetBonePreviewModel("root/torso/upper/arms/right", new Vector3(0f, -armTopLength, 0f), new Vector3(0.65f, armTopLength, 1f));

			var forearmLength = 1.25f;
			_skeleton.AddBone("root/torso/upper/arms/left/forearm", (new Vector3(0, -armTopLength - forearmLength, 0)));
			_skeleton.AddBone("root/torso/upper/arms/right/forearm", (new Vector3(0, -armTopLength - forearmLength, 0)));
			_skeleton.SetBonePreviewModel("root/torso/upper/arms/right/forearm", new Vector3(0f, -forearmLength, 0f), new Vector3(0.65f, forearmLength, 1f));
			_skeleton.SetBonePreviewModel("root/torso/upper/arms/left/forearm", new Vector3(0f, -forearmLength, 0f), new Vector3(0.65f, forearmLength, 1f));

			_skeleton.AddBone("root/torso/upper/arms/left/forearm/hand", (new Vector3(0, -forearmLength * 2 - 0.5f, 0)));
			_skeleton.AddBone("root/torso/upper/arms/right/forearm/hand", (new Vector3(0, -forearmLength * 2 - 0.5f, 0)));
			_skeleton.SetBonePreviewModel("root/torso/upper/arms/right/forearm/hand", Vector3.Zero, new Vector3(1f, 1f, 1f));
			_skeleton.SetBonePreviewModel("root/torso/upper/arms/left/forearm/hand", Vector3.Zero, new Vector3(1f, 1f, 1f));

			_skeleton.AddBone("root/torso/upper/head", (new Vector3(0, torsoHeight + 1f, 0)));
			_skeleton.AddBone("root/torso/upper/head/hair", (new Vector3(0, 1f, 0)));
			_skeleton.SetBonePreviewModel("root/torso/upper/head", Vector3.Zero, new Vector3(1f, 1f, 1f));

			// set starting bones
			UpdatePartsList();

			// reset filename
			UpdateFilename(null);
		}


		bool _freezeMeshUpdates = false;

		// tuple to convert bone index to path
		Tuple<string, string>[] _bonesIndexToPath;

		/// <summary>
		/// Update bones list.
		/// </summary>
		private void UpdatePartsList(string selectedBone = null)
		{
			// freeze updates
			_freezeMeshUpdates = true;

			_selectedBone = null;

			_bonesAndTransformsPanel.BonesListBox.Items.Clear();

			// get all bones (tuple of <display, full_string>)
			var bones = _bonesIndexToPath = _skeleton.GetFlatDisplayList();

			// update list
			int index = 0;
			_fullPathToBoneListIndex = new Dictionary<string, int>();
			//_bonesList.ClearItems();
			foreach(var path in bones)
			{
				var displayName = path.Item1;
				var fullPath = path.Item2;

				// Calculate indentation based on the hierarchy level
				int indentLevel = fullPath.Count(c => c == '/');
				var listBoxItem = new ListBoxItem
				{
					Content = displayName,
					Margin = new Thickness(indentLevel * 20, 0, 0, 0)
				};

				//_bonesList.AddItem(path.Item1);
				_fullPathToBoneListIndex[fullPath] = index++;
				_bonesAndTransformsPanel.BonesListBox.Items.Add(listBoxItem);
			}

			_timelineControl.AnimationSelector.Items.Clear();
			foreach(var animation in _skeleton.Animations.OrderBy(x => x))
			{
				var listBoxItem = new ComboBoxItem
				{
					Content = animation
				};
				_timelineControl.AnimationSelector.Items.Add(listBoxItem);
			}
			_timelineControl.AnimationSelector.SelectedIndex = 0;

			// unfreeze updates
			_freezeMeshUpdates = false;
		}

		/// <summary>
		/// Update all transformations from skeleton.
		/// </summary>
		void UpdateTransformationsFromSkeleton()
		{
			_freezeMeshUpdates = true;
			var meshTrans = _skeleton.GetPreviewModelTransform(_selectedBone);
			_freezeMeshUpdates = false;
		}

		/// <summary>
		/// Reset camera properties.
		/// </summary>
		void ResetCamera()
		{
			_cameraDistance = 35;
			_cameraAngle = new Vector2(MathF.PI / 2, MathF.PI / 4);
			_cameraOffsetY = 5;
		}

		/// <summary>
		/// Show help message.
		/// </summary>
		void PressedTopMenu_ShowHelp()
		{
		}

		/// <summary>
		/// Show about message.
		/// </summary>
		void PressedTopMenu_ShowAbout()
		{
		}

		/// <summary>
		/// Show / hide bones handles.
		/// </summary>
		void PressedTopMenu_ToggleShowHandlers(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
		{
			_showBoneHandles = !_showBoneHandles;
			context.Entity.ChangeItem(context.ItemIndex, (_showBoneHandles ? "{{L_GREEN}}" : "") + "Show Handles");
		}

		/// <summary>
		/// Show / hide lighting.
		/// </summary>
		void PressedTopMenu_ToggleLighting(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
		{
			_showLights = !_showLights;
			context.Entity.ChangeItem(context.ItemIndex, (_showLights ? "{{L_GREEN}}" : "") + "Enable Lighting");
		}

		/// <summary>
		/// Show / hide bones outline.
		/// </summary>
		void PressedTopMenu_ToggleShowOutline(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
		{
			_showBonesOutline = !_showBonesOutline;
			context.Entity.ChangeItem(context.ItemIndex, (_showBonesOutline ? "{{L_GREEN}}" : "") + "Bones Outline");
		}

		/// <summary>
		/// Show / hide bones.
		/// </summary>
		void PressedTopMenu_ToggleShowBones(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
		{
			_showBones = !_showBones;
			context.Entity.ChangeItem(context.ItemIndex, (_showBones ? "{{L_GREEN}}" : "") + "Show Bones");
		}

		/// <summary>
		/// Pressed top menu exit.
		/// </summary>
		void PressedTopMenu_Exit(object sender, RoutedEventArgs e)
		{
			ConfirmAndExit();
		}

		/// <summary>
		/// Ask for confirmation and exit app.
		/// </summary>
		void ConfirmAndExit()
		{
			if(GeonBit.UI.Utils.MessageBox.OpenedMsgBoxesCount == 0)
			{
				GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Exit Editor?", "Are you sure you wish to exit editor and discard any unsaved changes?", () => { Exit(); return true; }, null);
			}
		}

		/// <summary>
		/// Pressed top menu new.
		/// </summary>
		void PressedTopMenu_New(object sender, RoutedEventArgs e)
		{
			GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Discard Changes?", "Are you sure you wish to create a new skeleton and discard any unsaved changes?", () => { CreateEmptySkeleton(); return true; }, null);
		}

		/// <summary>
		/// Pressed top menu save-as.
		/// </summary>
		void PressedTopMenu_SaveAs(object sender, RoutedEventArgs e)
		{
			AskForNewSaveNameAndSave();
		}

		/// <summary>
		/// Pressed top menu save.
		/// </summary>
		void PressedTopMenu_Save(object sender, RoutedEventArgs e)
		{
			// existing save
			if(_currentFilename != null)
			{
				DoSave(_currentFilename);
			}
			// new save
			else
			{
				AskForNewSaveNameAndSave();
			}
		}

		/// <summary>
		/// Open dialog to pick new save name and save.
		/// </summary>
		void AskForNewSaveNameAndSave()
		{
			var textInput = new TextInput(false);
			textInput.PlaceholderText = "New Filename";
			GeonBit.UI.Utils.MessageBox.ShowMsgBox("New Save File", "Please enter save name:",
				new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
						new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Save", () =>
						{
							var filename = textInput.Value;
							if (filename != null)
							{
								if (!filename.ToLower().EndsWith(".ini")) { filename += ".ini"; }
								DoSaveWithValidations(filename, true);
							}
							return !string.IsNullOrEmpty(textInput.Value);
						}),
						new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
				}, new Entity[] { textInput });
		}

		/// <summary>
		/// Do saving with validations.
		/// </summary>
		void DoSaveWithValidations(string filename, bool confirmOverride)
		{
			// confirm override
			if(confirmOverride)
			{
				var fullPath = System.IO.Path.Combine(_savesFolder, filename);
				if(System.IO.File.Exists(fullPath))
				{
					GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Override existing file?", $"File named '{filename}' already exists. Override it?",
						() => { DoSave(filename); return true; }, () => { return true; });
					return;
				}
			}

			// no need for validation
			DoSave(filename);
		}

		/// <summary>
		/// Actually do the saving action.
		/// </summary>
		void DoSave(string filename)
		{
			var ini = Sini.IniFile.CreateEmpty();
			_skeleton.SaveTo(ini);
			ini.SaveTo(System.IO.Path.Combine(_savesFolder, filename));
			GeonBit.UI.Utils.MessageBox.ShowMsgBox("Saved Successfully!", $"Skeleton was saved as '{filename}'.");
			UpdateFilename(filename);
		}

		/// <summary>
		/// Update current project filename.
		/// </summary>
		void UpdateFilename(string newFilename)
		{
			if(newFilename == null)
			{
				Window.Title = "MonoSkelly - * Untitled *";
			}
			else
			{
				Window.Title = "MonoSkelly - " + newFilename;
			}
			_currentFilename = newFilename;
		}

		/// <summary>
		/// Pressed top menu save level.
		/// </summary>
		/// <summary>
		/// Pressed top menu save level.
		/// </summary>
		void PressedTopMenu_Load(object sender, RoutedEventArgs e)
		{
			// Create and show the LoadFileWindow
			var loadFileWindow = new LoadFileWindow();

			var files = System.IO.Directory.GetFiles(_savesFolder);
			List<string> list = new List<string>();
			foreach(var file in files)
			{
				if(file.ToLower().EndsWith(".ini"))
				{
					list.Add(System.IO.Path.GetFileName(file));
				}
			}
			loadFileWindow.FileDropdown.ItemsSource = list;

			// Set the first item as selected by default
			if(list.Count > 0)
			{
				loadFileWindow.FileDropdown.SelectedIndex = 0;
			}

			loadFileWindow.Owner = _mainWindow;
			if(loadFileWindow.ShowDialog() == true)
			{
				// Get the selected file from the dropdown
				var selectedFile = loadFileWindow.FileDropdown.SelectedItem as string;
				if(selectedFile != null)
				{
					LoadProject(selectedFile);
				}
			}
		}



		private void LoadFileWindow_StateChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		public void CallProtectedLoadContent()
		{
			LoadContent();
		}

		/// <summary>
		/// Load editor content.
		/// </summary>
		protected override void LoadContent()
		{
			// load basic models
			_spriteBatch = new SpriteBatch(GraphicsDevice);
			var boneModel = Content.Load<Model>("Editor\\bone");
			var handleModel = Content.Load<Model>("Editor\\handle");
			_floorModel = Content.Load<Model>("Editor\\plane");
			_floorTexture = Content.Load<Texture2D>("Editor\\grid");
			var boneTexture = Content.Load<Texture2D>("Editor\\bone_texture");
			Skeleton.SetDebugModels(handleModel, boneModel, boneTexture);

			// create default skeleton
			CreateEmptySkeleton();
		}

		/// <summary>
		/// Are we currently interacting with UI?
		/// </summary>
		public static bool IsInteractingWithUI => ((UserInterface.Active.TargetEntity != null) && (UserInterface.Active.TargetEntity != UserInterface.Active.Root));

		// time until we advance animation step
		double _timeForNextAdvanceStep = 0;

		public void CallProtectedUpdate(GameTime gameTime)
		{
			Update(gameTime);
		}


		private void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			// Update camera distance based on mouse wheel delta
			_cameraDistance -= e.Delta / 120f * 10f;
			if(_cameraDistance < 10) _cameraDistance = 10;
			if(_cameraDistance > 500) _cameraDistance = 500;
		}

		protected override void Update(GameTime gameTime)
		{
			// exit app
			if(Keyboard.IsKeyDown(Key.Escape))
			{
				ConfirmAndExit();
			}

			// get mouse movement
			var mouseX = (float)Mouse.GetPosition(_mainWindow).X;
			var mouseY = (float)Mouse.GetPosition(_mainWindow).Y;
			var mouseMove = new Vector2(mouseX - _prevMousePos.X, mouseY - _prevMousePos.Y);
			_prevMousePos = new Vector2(mouseX, mouseY);

			// do dragging to change value
			if(_draggedInput != null)
			{
				if(Mouse.LeftButton == MouseButtonState.Pressed)
				{
					if(_draggedTime > 0.15)
					{
						try
						{
							float curr = float.Parse(_draggedInput.Value);
							_draggedInput.Value = MathF.Round((curr + mouseMove.X / 10f), 2).ToString();
						}
						catch
						{
							_draggedInput.Value = "0";
						}
						_draggedInput.OnValueChange(_draggedInput);
					}
					_draggedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else
				{
					_draggedInput = null;
					_draggedTime = 0;
				}
			}


			// raycast to select bone
			if((Mouse.LeftButton == MouseButtonState.Pressed) && (_draggedInput == null))
			{
				//var animationId = SelectedAnimation;
				//var animationOffset = AnimationOffset;
				var animationId = "";
				float animationOffset = 0;
				var ray = _camera.RayFromMouse();
				var picked = _skeleton.PickBone(ray, Matrix.Identity, animationId, animationOffset);
				if(picked != null)
				{
					//_bonesList.SelectedIndex = _fullPathToBoneListIndex[picked];
				}
			}

			// do camera rotation
			if(Mouse.RightButton == MouseButtonState.Pressed)
			{
				var rotation = mouseMove;
				_cameraAngle.X += rotation.X * (float)gameTime.ElapsedGameTime.TotalSeconds * 3.5f;
				_cameraAngle.Y += rotation.Y * (float)gameTime.ElapsedGameTime.TotalSeconds * 1.5f;
			}

			// do camera offsetY change
			if(Mouse.MiddleButton == MouseButtonState.Pressed)
			{
				_cameraOffsetY += mouseMove.Y * (float)gameTime.ElapsedGameTime.TotalSeconds * 7.5f;
				if(_cameraOffsetY < -25) _cameraOffsetY = -25;
				if(_cameraOffsetY > 50) _cameraOffsetY = 50;
			}

			// validate camera angle
			if(_cameraAngle.Y < -MathF.PI) { _cameraAngle.Y = -MathF.PI; }
			if(_cameraAngle.Y > MathF.PI) { _cameraAngle.Y = MathF.PI; }

			// set camera position and lookat
			var cameraPositionXZFromRotation = new Vector2((float)Math.Cos(_cameraAngle.X), (float)Math.Sin(_cameraAngle.X));
			_camera.LookAt = new Vector3(0, _cameraOffsetY, 0);
			_camera.Position = new Vector3(
				-(cameraPositionXZFromRotation.X * _cameraDistance),
				_cameraOffsetY + (_cameraAngle.Y * _cameraDistance / 1.175f),
				(cameraPositionXZFromRotation.Y * _cameraDistance));
			_camera.FarClipPlane = _cameraDistance * 5;

			// update camera
			_camera.Update();

			base.Update(gameTime);
		}



		/// <summary>
		/// Draw scene.
		/// </summary>
		public void CallProtectedDraw(GameTime gameTime)
		{
			Draw(gameTime);
		}

		protected override void EndDraw()
		{
			// Disable this
			//Platform.Present();
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// draw floor
			var depthState = new DepthStencilState();
			depthState.DepthBufferEnable = true;
			depthState.DepthBufferWriteEnable = true;
			foreach(var mesh in _floorModel.Meshes)
			{
				foreach(BasicEffect effect in mesh.Effects)
				{
					effect.LightingEnabled = _showLights;
					effect.EnableDefaultLighting();
					effect.SpecularColor = Color.Black.ToVector3();
					effect.GraphicsDevice.DepthStencilState = depthState;
					effect.View = _camera.View;
					effect.Projection = _camera.Projection;
					effect.Texture = _floorTexture;
					effect.TextureEnabled = true;
					effect.World = Matrix.CreateRotationX(-MathF.PI / 2) * Matrix.CreateScale(50f, 1f, 50f);
				}
				mesh.Draw();
			}

			// get animation and offset
			string animationId = _animation ? _animation.Name : "";
			float animationOffset = (float)(_timelineControl.CurrentFrame / _timelineControl.Frames);

			// Update the timeline which will in turn call OnPlaybackFrame
			_timelineControl.Update(gameTime);

			if(_showBones)
			{
				_skeleton.DebugDrawBones(_camera.View, _camera.Projection, Matrix.Identity, animationId, animationOffset, _showBonesOutline, _selectedBone, _showLights);
			}

			// draw bone handles
			if(_showBoneHandles)
			{
				_skeleton.DebugDrawBoneHandles(_camera.View, _camera.Projection, Matrix.Identity, animationId, animationOffset, 0.2f, Color.Teal, Color.Red, _selectedBone);
			}

			// draw ui and call base draw
			//UserInterface.Active.Draw(_spriteBatch);
			base.Draw(gameTime);
		}
	}
}
