using DataStructures;
using OpenGl;
using System;
using Gdk;

public class TilemapEditor : IEditor, IDisposable {
	private Selection selection;
	private bool drawing;
	private bool selecting;	
	private FieldPos MouseTilePos;
	private FieldPos SelectStartPos;
	private FieldPos LastDrawPos;
	private FieldPos SelectionP1;
	private FieldPos SelectionP2;

	private Tilemap Tilemap;
	private Tileset Tileset;

	public event RedrawEventHandler Redraw;

	public TilemapEditor(Tilemap Tilemap, Tileset Tileset, Selection selection)
	{
		this.Tilemap = Tilemap;
		this.Tileset = Tileset;
		this.selection = selection;
		selection.Changed += OnSelectionChanged;
	}

	public void Dispose()
	{
		selection.Changed -= OnSelectionChanged;
	}

	public void Draw()
	{
		if(!selecting) {
			gl.Color4f(1, 1, 1, 0.7f);
			Vector pos = new Vector(MouseTilePos.X * 32f, MouseTilePos.Y * 32f);
			selection.Draw(pos, Tileset);
			gl.Color4f(1, 1, 1, 1);
		}
		if(selecting) {
			gl.Color4f(0, 0, 1, 0.7f);
			gl.Disable(gl.TEXTURE_2D);

			float left = SelectionP1.X * 32f;
			float top = SelectionP1.Y * 32f;
			float right = SelectionP2.X * 32f + 32f;
			float bottom = SelectionP2.Y * 32f + 32f;
			
			gl.Begin(gl.QUADS);
			gl.Vertex2f(left, top);
			gl.Vertex2f(right, top);
			gl.Vertex2f(right, bottom);
			gl.Vertex2f(left, bottom);
			gl.End();
			
			gl.Enable(gl.TEXTURE_2D);
			gl.Color4f(1, 1, 1, 1);
		}
	}

	public void OnMouseButtonPress(Vector MousePos, int button, ModifierType Modifiers)
	{
		UpdateMouseTilePos(MousePos);
	
		if(button == 1) {
			selection.ApplyToTilemap(MouseTilePos, Tilemap);
			LastDrawPos = MouseTilePos;
			drawing = true;
			Redraw();
		}
		if(button == 3) {
			if(MouseTilePos.X < 0 || MouseTilePos.Y < 0
				|| MouseTilePos.X >= Tilemap.Width
				|| MouseTilePos.Y >= Tilemap.Height)
				return;
			
			SelectStartPos = MouseTilePos;
			selecting = true;
			UpdateSelection();	
			Redraw();
		}
	}

	public void OnMouseButtonRelease(Vector MousePos, int button, ModifierType Modifiers)
	{
		UpdateMouseTilePos(MousePos);
	
		if(button == 1) {
			drawing = false;
		}
		if(button == 3) {
			UpdateSelection();

			uint NewWidth = (uint) (SelectionP2.X - SelectionP1.X) + 1;
			uint NewHeight = (uint) (SelectionP2.Y - SelectionP1.Y) + 1;
			selection.Resize(NewWidth, NewHeight, 0);
			for(uint y = 0; y < NewHeight; y++) {
				for(uint x = 0; x < NewWidth; ++x) {
					selection[x, y] 
						= Tilemap[(uint) SelectionP1.X + x,
						  		  (uint) SelectionP1.Y + y];
				}
			}

			selection.FireChangedEvent();
			selecting = false;
		}

		Redraw();
	}

	public void OnMouseMotion(Vector MousePos, ModifierType Modifiers)
	{
		if(UpdateMouseTilePos(MousePos)) {
			if(selection.Width == 0 || selection.Height == 0)
				return;

			if(drawing &&
					( (Modifiers & ModifierType.ShiftMask) != 0 ||
				((LastDrawPos.X - MouseTilePos.X) % selection.Width == 0 &&
				 (LastDrawPos.Y - MouseTilePos.Y) % selection.Height == 0))) {
				LastDrawPos = MouseTilePos;
				selection.ApplyToTilemap(MouseTilePos, Tilemap);
			}
			if(selecting)
				UpdateSelection();
			Redraw();
		}
	}

	private bool UpdateMouseTilePos(Vector MousePos)
	{
		FieldPos NewMouseTilePos = new FieldPos(
				(int) (MousePos.X) / 32,
				(int) (MousePos.Y) / 32);
		if(NewMouseTilePos != MouseTilePos) {
			MouseTilePos = NewMouseTilePos;
			return true;
		}

		return false;
	}

	private void UpdateSelection()
	{
		if(MouseTilePos.X < SelectStartPos.X) {
			if(MouseTilePos.X < 0)
				SelectionP1.X = 0;
			else
				SelectionP1.X = MouseTilePos.X;
			SelectionP2.X = SelectStartPos.X;
		} else {
			SelectionP1.X = SelectStartPos.X;
			if(MouseTilePos.X >= Tilemap.Width)
				SelectionP2.X = (int) Tilemap.Width - 1;
			else
				SelectionP2.X = MouseTilePos.X;
		}

		if(MouseTilePos.Y < SelectStartPos.Y) {
			if(MouseTilePos.Y < 0)
				SelectionP1.Y = 0;
			else
				SelectionP1.Y = MouseTilePos.Y;
			SelectionP2.Y = SelectStartPos.Y;
		} else {
			SelectionP1.Y = SelectStartPos.Y;
			if(MouseTilePos.Y >= Tilemap.Height)
				SelectionP2.Y = (int) Tilemap.Height - 1;
			else
				SelectionP2.Y = MouseTilePos.Y;
		}
	}
	
	private void OnSelectionChanged() {
		Redraw();
	}
}