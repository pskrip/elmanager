using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Elmanager.Forms;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace Elmanager.EditorTools
{
    internal class SelectionTool : ToolBase, IEditorTool
    {
        private bool _anythingMoved;
        private Vector _lockCenter; //for lock lines -mode
        private Vector _lockNext; //for lock lines -mode
        private Vector _lockPrev; //for lock lines -mode
        private bool _lockingLines;
        private Vector _moveStartPosition;
        private Polygon _selectionPoly;

        private bool FreeSelecting { get; set; }

        private Vector _selectionStartPoint;
        private double _mouseTrip;
        private Vector _lastMousePosition;

        internal SelectionTool(LevelEditor editor)
            : base(editor)
        {
        }

        private bool Moving { get; set; }

        private bool RectSelecting { get; set; }

        public void Activate()
        {
            UpdateHelp();
        }

        public void ExtraRendering()
        {
            if (RectSelecting)
                Renderer.DrawRectangle(_selectionStartPoint, CurrentPos, Color.Blue);
            else if (FreeSelecting)
                Renderer.DrawPolygon(_selectionPoly, Color.Blue);
        }

        public List<Polygon> GetExtraPolygons()
        {
            return new List<Polygon>();
        }

        public void InActivate()
        {
            Moving = false;
            RectSelecting = false;
        }

        public void KeyDown(KeyEventArgs key)
        {
            switch (key.KeyCode)
            {
                case Keys.Space:
                    LevEditor.TransformMenuItemClick();
                    break;
                case Keys.D1:
                    UpdateAnimNumbers(1);
                    break;
                case Keys.D2:
                    UpdateAnimNumbers(2);
                    break;
                case Keys.D3:
                    UpdateAnimNumbers(3);
                    break;
                case Keys.D4:
                    UpdateAnimNumbers(4);
                    break;
                case Keys.D5:
                    UpdateAnimNumbers(5);
                    break;
                case Keys.D6:
                    UpdateAnimNumbers(6);
                    break;
                case Keys.D7:
                    UpdateAnimNumbers(7);
                    break;
                case Keys.D8:
                    UpdateAnimNumbers(8);
                    break;
                case Keys.D9:
                    UpdateAnimNumbers(9);
                    break;
            }
        }

        private void UpdateAnimNumbers(int animNum)
        {
            var selected = Lev.Objects.Where(obj =>
                obj.Type == ObjectType.Apple && obj.Position.Mark == VectorMark.Selected).ToList();
            bool updated = selected.Any(obj => obj.AnimationNumber != animNum);
            selected.ForEach(obj => obj.AnimationNumber = animNum);
            if (updated)
            {
                LevEditor.Modified = true;
            }

            ShowObjectInfo(GetNearestObjectIndex(CurrentPos));
        }

        public void MouseDown(MouseEventArgs mouseData)
        {
            Vector p = CurrentPos;
            _anythingMoved = false;
            int nearestVertexIndex = GetNearestVertexIndex(p);
            int nearestObjectIndex = GetNearestObjectIndex(p);
            int nearestPictureIndex = GetNearestPictureIndex(p);
            switch (mouseData.Button)
            {
                case MouseButtons.Left:
                    if (nearestVertexIndex >= -1 && Keyboard.IsKeyDown(Key.LeftAlt))
                    {
                        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            MarkAllAs(VectorMark.None);
                        }

                        NearestPolygon.MarkVectorsAs(VectorMark.Selected);
                        var inearest = NearestPolygon.ToIPolygon();
                        foreach (var polygon in Lev.Polygons.Where(polygon => polygon.ToIPolygon().Within(inearest)))
                        {
                            polygon.MarkVectorsAs(VectorMark.Selected);
                        }

                        foreach (var obj in Lev.Objects)
                        {
                            if (NearestPolygon.AreaHasPoint(obj.Position))
                            {
                                obj.Position.Mark = VectorMark.Selected;
                            }
                        }

                        foreach (var pic in Lev.Pictures)
                        {
                            if (NearestPolygon.AreaHasPoint(pic.Position))
                            {
                                pic.Position.Mark = VectorMark.Selected;
                            }
                        }

                        EndSelectionHandling();
                    }
                    else if (nearestVertexIndex >= 0)
                    {
                        HandleMark(NearestPolygon[nearestVertexIndex]);
                        if (Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            _lockCenter = NearestPolygon[nearestVertexIndex];
                            _lockPrev = NearestPolygon[nearestVertexIndex - 1];
                            _lockNext = NearestPolygon[nearestVertexIndex + 1];
                            _lockingLines = true;
                            _moveStartPosition = _lockCenter;
                        }
                    }
                    else if (nearestVertexIndex == -1)
                    {
                        int nearestSegmentIndex = NearestPolygon.GetNearestSegmentIndex(p);
                        AdjustForGrid(p);
                        if (Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            MarkAllAs(VectorMark.None);
                            p.Mark = VectorMark.Selected;
                            NearestPolygon.Insert(nearestSegmentIndex + 1, p);
                            LevEditor.Modified = true;
                        }
                        else
                        {
                            if (
                                !(NearestPolygon[nearestSegmentIndex].Mark == VectorMark.Selected &&
                                  NearestPolygon[nearestSegmentIndex + 1].Mark == VectorMark.Selected))
                            {
                                if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                                {
                                    MarkAllAs(VectorMark.None);
                                    NearestPolygon.MarkVectorsAs(VectorMark.Selected);
                                }
                            }

                            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                            {
                                NearestPolygon.MarkVectorsAs(
                                    NearestPolygon.Vertices.TrueForAll(v => v.Mark == VectorMark.Selected)
                                        ? VectorMark.None
                                        : VectorMark.Selected);
                            }
                        }

                        EndSelectionHandling();
                    }
                    else if (nearestObjectIndex >= 0)
                        HandleMark(Lev.Objects[nearestObjectIndex].Position);
                    else if (nearestPictureIndex >= 0)
                        HandleMark(Lev.Pictures[nearestPictureIndex].Position);
                    else
                    {
                        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            MarkAllAs(VectorMark.None);
                            LevEditor.PreserveSelection();
                        }

                        if (Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            FreeSelecting = true;
                            _selectionPoly = new Polygon();
                            _selectionPoly.Add(CurrentPos);
                            _mouseTrip = 0;
                            _lastMousePosition = CurrentPos;
                        }
                        else
                        {
                            _selectionStartPoint = p;
                            RectSelecting = true;
                        }
                    }

                    LevEditor.UpdateSelectionInfo();
                    break;
                case MouseButtons.Right:
                    break;

                case MouseButtons.Middle:
                    break;
            }
        }

        public void MouseMove(Vector p)
        {
            CurrentPos = p;
            if (Moving)
            {
                AdjustForGrid(p);
                if (_lockingLines)
                {
                    p = GeometryUtils.OrthogonalProjection(_lockCenter,
                        GeometryUtils.DistanceFromLine(_lockCenter, _lockNext, p) <
                        GeometryUtils.DistanceFromLine(_lockCenter, _lockPrev, p)
                            ? _lockNext
                            : _lockPrev, p);
                }

                Vector delta = p - _moveStartPosition;
                if (delta.Length > 0)
                    _anythingMoved = true;
                Vector.MarkDefault = VectorMark.Selected;
                foreach (Polygon x in Lev.Polygons)
                {
                    bool anythingMoved = false;
                    for (int i = 0; i < x.Vertices.Count; i++)
                    {
                        if (x.Vertices[i].Mark != VectorMark.Selected) continue;
                        x.Vertices[i] += delta;
                        anythingMoved = true;
                    }

                    if (anythingMoved)
                        x.UpdateDecomposition();
                }

                foreach (LevObject x in Lev.Objects.Where(x => x.Position.Mark == VectorMark.Selected))
                {
                    x.Position += delta;
                }

                foreach (Picture z in Lev.Pictures.Where(z => z.Position.Mark == VectorMark.Selected))
                {
                    z.Position += delta;
                }

                Vector.MarkDefault = VectorMark.None;
                _moveStartPosition = p;
            }
            else if (FreeSelecting)
            {
                _mouseTrip += (p - _lastMousePosition).Length;
                _lastMousePosition = p;
                double step = 0.02 * ZoomCtrl.ZoomLevel;
                if (_mouseTrip > step)
                {
                    while (!(_mouseTrip < step))
                        _mouseTrip -= step;
                    _selectionPoly.Add(p);
                }
            }
            else if (!Busy)
            {
                ResetHighlight();
                int nearestVertex = GetNearestVertexIndex(p);
                int nearestObject = GetNearestObjectIndex(p);
                int nearestTextureIndex = GetNearestPictureIndex(p);
                if (nearestVertex == -1)
                {
                    ChangeCursorToHand();
                    NearestPolygon.Mark = PolygonMark.Highlight;
                    LevEditor.HighlightLabel.Text = NearestPolygon.IsGrass ? "Grass" : "Ground";
                    LevEditor.HighlightLabel.Text += " polygon, " + NearestPolygon.Count + " vertices";
                }
                else if (nearestVertex >= 0)
                {
                    ChangeCursorToHand();
                    Vector b = NearestPolygon.Vertices[nearestVertex];
                    if (b.Mark == VectorMark.None)
                        b.Mark = VectorMark.Highlight;
                    LevEditor.HighlightLabel.Text = NearestPolygon.IsGrass ? "Grass" : "Ground";
                    LevEditor.HighlightLabel.Text += " polygon, vertex " + (nearestVertex + 1) + " of " +
                                                     NearestPolygon.Count + " vertices";
                }
                else if (nearestObject >= 0)
                {
                    ChangeCursorToHand();
                    if (Lev.Objects[nearestObject].Position.Mark == VectorMark.None)
                        Lev.Objects[nearestObject].Position.Mark = VectorMark.Highlight;
                    ShowObjectInfo(nearestObject);
                }
                else if (nearestTextureIndex >= 0)
                {
                    ChangeCursorToHand();
                    if (Lev.Pictures[nearestTextureIndex].Position.Mark == VectorMark.None)
                        Lev.Pictures[nearestTextureIndex].Position.Mark = VectorMark.Highlight;
                    ShowTextureInfo(nearestTextureIndex);
                }
                else
                {
                    ChangeToDefaultCursorIfHand();
                    LevEditor.HighlightLabel.Text = "";
                }
            }
        }

        public void MouseOutOfEditor()
        {
            ResetHighlight();
        }

        public void MouseUp()
        {
            if (RectSelecting || FreeSelecting)
            {
                double selectionxMin = 0;
                double selectionxMax = 0;
                double selectionyMax = 0;
                double selectionyMin = 0;
                if (RectSelecting)
                {
                    selectionxMin = Math.Min(CurrentPos.X, _selectionStartPoint.X);
                    selectionxMax = Math.Max(CurrentPos.X, _selectionStartPoint.X);
                    selectionyMax = Math.Max(CurrentPos.Y, _selectionStartPoint.Y);
                    selectionyMin = Math.Min(CurrentPos.Y, _selectionStartPoint.Y);
                }

                var grassFilter = LevEditor.EffectiveGrassFilter;
                var groundFilter = LevEditor.EffectiveGroundFilter;
                var appleFilter = LevEditor.EffectiveAppleFilter;
                var killerFilter = LevEditor.EffectiveKillerFilter;
                var flowerFilter = LevEditor.EffectiveFlowerFilter;
                var pictureFilter = LevEditor.EffectivePictureFilter;
                var textureFilter = LevEditor.EffectiveTextureFilter;
                foreach (Polygon x in Lev.Polygons)
                {
                    if ((x.IsGrass && grassFilter) || (!x.IsGrass && groundFilter))
                    {
                        foreach (Vector t in x.Vertices)
                            if (RectSelecting)
                                MarkSelectedInArea(t, selectionxMin, selectionxMax, selectionyMin, selectionyMax);
                            else
                            {
                                MarkSelectedInArea(t, _selectionPoly);
                            }
                    }
                }

                foreach (LevObject t in Lev.Objects)
                {
                    ObjectType type = t.Type;
                    if (type == ObjectType.Start || (type == ObjectType.Apple && appleFilter) ||
                        (type == ObjectType.Killer && killerFilter) ||
                        (type == ObjectType.Flower && flowerFilter))
                        if (RectSelecting)
                            MarkSelectedInArea(t.Position, selectionxMin, selectionxMax, selectionyMin,
                                selectionyMax);
                        else
                        {
                            MarkSelectedInArea(t.Position, _selectionPoly);
                        }
                }

                foreach (Picture z in Lev.Pictures)
                {
                    if ((z.IsPicture && pictureFilter) || (!z.IsPicture && textureFilter))
                        if (RectSelecting)
                            MarkSelectedInArea(z.Position, selectionxMin, selectionxMax, selectionyMin, selectionyMax);
                        else
                        {
                            MarkSelectedInArea(z.Position, _selectionPoly);
                        }
                }

                LevEditor.UpdateSelectionInfo();
                LevEditor.PreserveSelection();
                RectSelecting = false;
                FreeSelecting = false;
            }
            else if (Moving)
            {
                Moving = false;
                _lockingLines = false;
                if (_anythingMoved)
                    LevEditor.Modified = true;
            }
        }

        private static void MarkSelectedInArea(Vector z, Polygon selectionPoly)
        {
            if (selectionPoly.AreaHasPoint(z))
            {
                switch (z.Mark)
                {
                    case VectorMark.None:
                        z.Mark = VectorMark.Selected;
                        break;
                    case VectorMark.Selected:
                        z.Mark = VectorMark.None;
                        break;
                }
            }
            else if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                z.Mark = VectorMark.None;
        }

        public void UpdateHelp()
        {
            LevEditor.InfoLabel.Text = "Left mouse button: select level elements; Left Shift: Bend edge";
        }

        private static void MarkSelectedInArea(Vector z, double selectionxMin, double selectionxMax,
            double selectionyMin, double selectionyMax)
        {
            if (z.X < selectionxMax && z.X > selectionxMin && z.Y < selectionyMax && z.Y > selectionyMin)
            {
                switch (z.Mark)
                {
                    case VectorMark.None:
                        z.Mark = VectorMark.Selected;
                        break;
                    case VectorMark.Selected:
                        z.Mark = VectorMark.None;
                        break;
                }
            }
            else if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                z.Mark = VectorMark.None;
        }

        private void HandleMark(Vector v)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (v.Mark != VectorMark.Selected)
                    MarkAllAs(VectorMark.None);
                v.Mark = VectorMark.Selected;
            }
            else
            {
                v.Mark = v.Mark != VectorMark.Selected
                    ? VectorMark.Selected
                    : VectorMark.None;
            }

            EndSelectionHandling();
        }

        private void EndSelectionHandling()
        {
            Moving = true;
            LevEditor.PreserveSelection();
            AdjustForGrid(CurrentPos);
            _moveStartPosition = CurrentPos;
        }

        private void ShowObjectInfo(int currentObjectIndex)
        {
            if (currentObjectIndex < 0)
            {
                return;
            }

            LevObject currObj = Lev.Objects[currentObjectIndex];
            switch (currObj.Type)
            {
                case ObjectType.Apple:
                    LevEditor.HighlightLabel.Text = "Apple: ";
                    switch (currObj.AppleType)
                    {
                        case AppleType.Normal:
                            LevEditor.HighlightLabel.Text += "Normal";
                            break;
                        case AppleType.GravityUp:
                            LevEditor.HighlightLabel.Text += "Gravity up";
                            break;
                        case AppleType.GravityDown:
                            LevEditor.HighlightLabel.Text += "Gravity down";
                            break;
                        case AppleType.GravityLeft:
                            LevEditor.HighlightLabel.Text += "Gravity left";
                            break;
                        case AppleType.GravityRight:
                            LevEditor.HighlightLabel.Text += "Gravity right";
                            break;
                    }

                    LevEditor.HighlightLabel.Text += ", animation number: " + currObj.AnimationNumber;
                    break;
                case ObjectType.Killer:
                    LevEditor.HighlightLabel.Text = "Killer";
                    break;
                case ObjectType.Flower:
                    LevEditor.HighlightLabel.Text = "Flower";
                    break;
                case ObjectType.Start:
                    LevEditor.HighlightLabel.Text = "Start";
                    break;
            }
        }

        private void ShowTextureInfo(int index)
        {
            Picture picture = Lev.Pictures[index];
            if (picture.IsPicture)
                LevEditor.HighlightLabel.Text = "Picture: " + picture.Name +
                                                ", distance: " +
                                                picture.Distance + ", clipping: " + picture.Clipping;
            else
            {
                LevEditor.HighlightLabel.Text = "Texture: " + picture.TextureName + ", mask: " + picture.Name +
                                                ", distance: " +
                                                picture.Distance + ", clipping: " + picture.Clipping;
            }
        }

        public override bool Busy => RectSelecting || FreeSelecting || Moving;
    }
}