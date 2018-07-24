/*

This file is part of the iText (R) project.
Copyright (c) 1998-2018 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using System.Text;
using iText.IO.Log;
using iText.IO.Util;
using iText.Kernel.Geom;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Margincollapse;
using iText.Layout.Minmaxwidth;
using iText.Layout.Properties;

namespace iText.Layout.Renderer {
    /// <summary>
    /// This class represents the
    /// <see cref="IRenderer">renderer</see>
    /// object for a
    /// <see cref="iText.Layout.Element.Paragraph"/>
    /// object. It will draw the glyphs of the textual content on the
    /// <see cref="DrawContext"/>
    /// .
    /// </summary>
    public class ParagraphRenderer : BlockRenderer {
        protected internal float previousDescent = 0;

        protected internal IList<LineRenderer> lines = null;

        /// <summary>Creates a ParagraphRenderer from its corresponding layout object.</summary>
        /// <param name="modelElement">
        /// the
        /// <see cref="iText.Layout.Element.Paragraph"/>
        /// which this object should manage
        /// </param>
        public ParagraphRenderer(Paragraph modelElement)
            : base(modelElement) {
        }

        /// <summary><inheritDoc/></summary>
        public override LayoutResult Layout(LayoutContext layoutContext) {
            bool wasHeightClipped = false;
            bool wasParentsHeightClipped = layoutContext.IsClippedHeight();
            int pageNumber = layoutContext.GetArea().GetPageNumber();
            bool anythingPlaced = false;
            bool firstLineInBox = true;
            LineRenderer currentRenderer = (LineRenderer)new LineRenderer().SetParent(this);
            Rectangle parentBBox = layoutContext.GetArea().GetBBox().Clone();
            MarginsCollapseHandler marginsCollapseHandler = null;
            bool marginsCollapsingEnabled = true.Equals(GetPropertyAsBoolean(Property.COLLAPSING_MARGINS));
            if (marginsCollapsingEnabled) {
                marginsCollapseHandler = new MarginsCollapseHandler(this, layoutContext.GetMarginsCollapseInfo());
            }
            bool notAllKidsAreFloats = false;
            IList<Rectangle> floatRendererAreas = layoutContext.GetFloatRendererAreas();
            FloatPropertyValue? floatPropertyValue = this.GetProperty<FloatPropertyValue?>(Property.FLOAT);
            float clearHeightCorrection = FloatingHelper.CalculateClearHeightCorrection(this, floatRendererAreas, parentBBox
                );
            FloatingHelper.ApplyClearance(parentBBox, marginsCollapseHandler, clearHeightCorrection, FloatingHelper.IsRendererFloating
                (this));
            float? blockWidth = RetrieveWidth(parentBBox.GetWidth());
            if (FloatingHelper.IsRendererFloating(this, floatPropertyValue)) {
                blockWidth = FloatingHelper.AdjustFloatedBlockLayoutBox(this, parentBBox, blockWidth, floatRendererAreas, 
                    floatPropertyValue);
                floatRendererAreas = new List<Rectangle>();
            }
            if (0 == childRenderers.Count) {
                anythingPlaced = true;
                currentRenderer = null;
            }
            bool isPositioned = IsPositioned();
            float? rotation = this.GetPropertyAsFloat(Property.ROTATION_ANGLE);
            OverflowPropertyValue? overflowX = this.GetProperty<OverflowPropertyValue?>(Property.OVERFLOW_X);
            float? blockMaxHeight = RetrieveMaxHeight();
            OverflowPropertyValue? overflowY = (null == blockMaxHeight || blockMaxHeight > parentBBox.GetHeight()) && 
                !wasParentsHeightClipped ? OverflowPropertyValue.FIT : this.GetProperty<OverflowPropertyValue?>(Property
                .OVERFLOW_Y);
            if (rotation != null || IsFixedLayout()) {
                parentBBox.MoveDown(AbstractRenderer.INF - parentBBox.GetHeight()).SetHeight(AbstractRenderer.INF);
            }
            if (rotation != null && !FloatingHelper.IsRendererFloating(this)) {
                blockWidth = RotationUtils.RetrieveRotatedLayoutWidth(parentBBox.GetWidth(), this);
            }
            if (marginsCollapsingEnabled) {
                marginsCollapseHandler.StartMarginsCollapse(parentBBox);
            }
            Border[] borders = GetBorders();
            float[] paddings = GetPaddings();
            float additionalWidth = ApplyBordersPaddingsMargins(parentBBox, borders, paddings);
            ApplyWidth(parentBBox, blockWidth, overflowX);
            wasHeightClipped = ApplyMaxHeight(parentBBox, blockMaxHeight, marginsCollapseHandler, false, wasParentsHeightClipped
                , overflowY);
            MinMaxWidth minMaxWidth = new MinMaxWidth(additionalWidth, layoutContext.GetArea().GetBBox().GetWidth());
            AbstractWidthHandler widthHandler = new MaxMaxWidthHandler(minMaxWidth);
            IList<Rectangle> areas;
            if (isPositioned) {
                areas = JavaCollectionsUtil.SingletonList(parentBBox);
            }
            else {
                areas = InitElementAreas(new LayoutArea(pageNumber, parentBBox));
            }
            occupiedArea = new LayoutArea(pageNumber, new Rectangle(parentBBox.GetX(), parentBBox.GetY() + parentBBox.
                GetHeight(), parentBBox.GetWidth(), 0));
            ShrinkOccupiedAreaForAbsolutePosition();
            int currentAreaPos = 0;
            Rectangle layoutBox = areas[0].Clone();
            lines = new List<LineRenderer>();
            foreach (IRenderer child in childRenderers) {
                notAllKidsAreFloats = notAllKidsAreFloats || !FloatingHelper.IsRendererFloating(child);
                currentRenderer.AddChild(child);
            }
            float lastYLine = layoutBox.GetY() + layoutBox.GetHeight();
            Leading leading = this.GetProperty<Leading>(Property.LEADING);
            float lastLineBottomLeadingIndent = 0;
            if (marginsCollapsingEnabled && childRenderers.Count > 0) {
                // passing null is sufficient to notify that there is a kid, however we don't care about it and it's margins
                marginsCollapseHandler.StartChildMarginsHandling(null, layoutBox);
            }
            while (currentRenderer != null) {
                currentRenderer.SetProperty(Property.TAB_DEFAULT, this.GetPropertyAsFloat(Property.TAB_DEFAULT));
                currentRenderer.SetProperty(Property.TAB_STOPS, this.GetProperty<Object>(Property.TAB_STOPS));
                float lineIndent = anythingPlaced ? 0 : (float)this.GetPropertyAsFloat(Property.FIRST_LINE_INDENT);
                float childBBoxWidth = layoutBox.GetWidth() - lineIndent;
                Rectangle childLayoutBox = new Rectangle(layoutBox.GetX() + lineIndent, layoutBox.GetY(), childBBoxWidth, 
                    layoutBox.GetHeight());
                currentRenderer.SetProperty(Property.OVERFLOW_X, overflowX);
                currentRenderer.SetProperty(Property.OVERFLOW_Y, overflowY);
                LineLayoutResult result = ((LineLayoutResult)((LineRenderer)currentRenderer.SetParent(this)).Layout(new LayoutContext
                    (new LayoutArea(pageNumber, childLayoutBox), null, floatRendererAreas, wasHeightClipped || wasParentsHeightClipped
                    )));
                if (result.GetStatus() == LayoutResult.NOTHING) {
                    float? lineShiftUnderFloats = FloatingHelper.CalculateLineShiftUnderFloats(floatRendererAreas, layoutBox);
                    if (lineShiftUnderFloats != null) {
                        layoutBox.DecreaseHeight((float)lineShiftUnderFloats);
                        firstLineInBox = true;
                        continue;
                    }
                }
                float minChildWidth = 0;
                float maxChildWidth = 0;
                if (result is MinMaxWidthLayoutResult) {
                    minChildWidth = ((MinMaxWidthLayoutResult)result).GetNotNullMinMaxWidth(childBBoxWidth).GetMinWidth();
                    maxChildWidth = ((MinMaxWidthLayoutResult)result).GetNotNullMinMaxWidth(childBBoxWidth).GetMaxWidth();
                }
                widthHandler.UpdateMinChildWidth(minChildWidth + lineIndent);
                widthHandler.UpdateMaxChildWidth(maxChildWidth + lineIndent);
                LineRenderer processedRenderer = null;
                if (result.GetStatus() == LayoutResult.FULL) {
                    processedRenderer = currentRenderer;
                }
                else {
                    if (result.GetStatus() == LayoutResult.PARTIAL) {
                        processedRenderer = (LineRenderer)result.GetSplitRenderer();
                    }
                }
                TextAlignment? textAlignment = (TextAlignment?)this.GetProperty<TextAlignment?>(Property.TEXT_ALIGNMENT, TextAlignment
                    .LEFT);
                if (result.GetStatus() == LayoutResult.PARTIAL && textAlignment == TextAlignment.JUSTIFIED && !result.IsSplitForcedByNewline
                    () || textAlignment == TextAlignment.JUSTIFIED_ALL) {
                    if (processedRenderer != null) {
                        processedRenderer.Justify(layoutBox.GetWidth() - lineIndent);
                    }
                }
                else {
                    if (textAlignment != TextAlignment.LEFT && processedRenderer != null) {
                        float deltaX = childBBoxWidth - processedRenderer.GetOccupiedArea().GetBBox().GetWidth();
                        switch (textAlignment) {
                            case TextAlignment.RIGHT: {
                                processedRenderer.Move(deltaX, 0);
                                break;
                            }

                            case TextAlignment.CENTER: {
                                processedRenderer.Move(deltaX / 2, 0);
                                break;
                            }
                        }
                    }
                }
                bool lineHasContent = processedRenderer != null && processedRenderer.GetOccupiedArea().GetBBox().GetHeight
                    () > 0;
                // could be false if e.g. line contains only floats
                bool doesNotFit = processedRenderer == null;
                float deltaY = 0;
                if (!doesNotFit) {
                    if (lineHasContent) {
                        float indentFromLastLine = previousDescent - lastLineBottomLeadingIndent - (leading != null ? processedRenderer
                            .GetTopLeadingIndent(leading) : 0) - processedRenderer.GetMaxAscent();
                        // TODO this is a workaround. To be refactored
                        if (processedRenderer != null && processedRenderer.ContainsImage()) {
                            indentFromLastLine += previousDescent;
                        }
                        deltaY = lastYLine + indentFromLastLine - processedRenderer.GetYLine();
                        lastLineBottomLeadingIndent = leading != null ? processedRenderer.GetBottomLeadingIndent(leading) : 0;
                        // TODO this is a workaround. To be refactored
                        if (lastLineBottomLeadingIndent < 0 && processedRenderer.ContainsImage()) {
                            lastLineBottomLeadingIndent = 0;
                        }
                    }
                    // for the first and last line in a paragraph, leading is smaller
                    if (firstLineInBox) {
                        deltaY = processedRenderer != null && leading != null ? -processedRenderer.GetTopLeadingIndent(leading) : 
                            0;
                    }
                    doesNotFit = leading != null && processedRenderer.GetOccupiedArea().GetBBox().GetY() + deltaY < layoutBox.
                        GetY();
                }
                if (doesNotFit && (null == processedRenderer || null == overflowY || OverflowPropertyValue.FIT.Equals(overflowY
                    ))) {
                    if (currentAreaPos + 1 < areas.Count) {
                        layoutBox = areas[++currentAreaPos].Clone();
                        lastYLine = layoutBox.GetY() + layoutBox.GetHeight();
                        firstLineInBox = true;
                    }
                    else {
                        bool keepTogether = IsKeepTogether();
                        if (keepTogether) {
                            return new MinMaxWidthLayoutResult(LayoutResult.NOTHING, null, null, this, null == result.GetCauseOfNothing
                                () ? this : result.GetCauseOfNothing());
                        }
                        else {
                            if (marginsCollapsingEnabled) {
                                if (anythingPlaced && notAllKidsAreFloats) {
                                    marginsCollapseHandler.EndChildMarginsHandling(layoutBox);
                                }
                                marginsCollapseHandler.EndMarginsCollapse(layoutBox);
                            }
                            iText.Layout.Renderer.ParagraphRenderer[] split = Split();
                            split[0].lines = lines;
                            foreach (LineRenderer line in lines) {
                                split[0].childRenderers.AddAll(line.GetChildRenderers());
                            }
                            if (processedRenderer != null) {
                                split[1].childRenderers.AddAll(processedRenderer.GetChildRenderers());
                            }
                            if (result.GetOverflowRenderer() != null) {
                                split[1].childRenderers.AddAll(result.GetOverflowRenderer().GetChildRenderers());
                            }
                            UpdateHeightsOnSplit(wasHeightClipped, this, split[1]);
                            CorrectPositionedLayout(layoutBox);
                            ApplyPaddings(occupiedArea.GetBBox(), paddings, true);
                            ApplyBorderBox(occupiedArea.GetBBox(), borders, true);
                            ApplyMargins(occupiedArea.GetBBox(), true);
                            ApplyAbsolutePositionIfNeeded(layoutContext);
                            LayoutArea editedArea = FloatingHelper.AdjustResultOccupiedAreaForFloatAndClear(this, layoutContext.GetFloatRendererAreas
                                (), layoutContext.GetArea().GetBBox(), clearHeightCorrection, marginsCollapsingEnabled);
                            if (wasHeightClipped) {
                                return new MinMaxWidthLayoutResult(LayoutResult.FULL, editedArea, split[0], null).SetMinMaxWidth(minMaxWidth
                                    );
                            }
                            else {
                                if (anythingPlaced) {
                                    return new MinMaxWidthLayoutResult(LayoutResult.PARTIAL, editedArea, split[0], split[1]).SetMinMaxWidth(minMaxWidth
                                        );
                                }
                                else {
                                    if (true.Equals(GetPropertyAsBoolean(Property.FORCED_PLACEMENT))) {
                                        occupiedArea.SetBBox(Rectangle.GetCommonRectangle(occupiedArea.GetBBox(), currentRenderer.GetOccupiedArea(
                                            ).GetBBox()));
                                        if (occupiedArea.GetBBox().GetWidth() > layoutBox.GetWidth() && !(null == overflowX || OverflowPropertyValue
                                            .FIT.Equals(overflowX))) {
                                            occupiedArea.GetBBox().SetWidth(layoutBox.GetWidth());
                                        }
                                        parent.SetProperty(Property.FULL, true);
                                        lines.Add(currentRenderer);
                                        // Force placement of children we have and do not force placement of the others
                                        if (LayoutResult.PARTIAL == result.GetStatus()) {
                                            IRenderer childNotRendered = result.GetCauseOfNothing();
                                            int firstNotRendered = currentRenderer.childRenderers.IndexOf(childNotRendered);
                                            currentRenderer.childRenderers.RetainAll(currentRenderer.childRenderers.SubList(0, firstNotRendered));
                                            split[1].childRenderers.RemoveAll(split[1].childRenderers.SubList(0, firstNotRendered));
                                            return new MinMaxWidthLayoutResult(LayoutResult.PARTIAL, editedArea, this, split[1], null).SetMinMaxWidth(
                                                minMaxWidth);
                                        }
                                        else {
                                            return new MinMaxWidthLayoutResult(LayoutResult.FULL, editedArea, null, null, this).SetMinMaxWidth(minMaxWidth
                                                );
                                        }
                                    }
                                    else {
                                        return new MinMaxWidthLayoutResult(LayoutResult.NOTHING, null, null, this, null == result.GetCauseOfNothing
                                            () ? this : result.GetCauseOfNothing());
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    if (leading != null) {
                        processedRenderer.ApplyLeading(deltaY);
                        if (lineHasContent) {
                            lastYLine = processedRenderer.GetYLine();
                        }
                    }
                    if (lineHasContent) {
                        occupiedArea.SetBBox(Rectangle.GetCommonRectangle(occupiedArea.GetBBox(), processedRenderer.GetOccupiedArea
                            ().GetBBox()));
                        if (occupiedArea.GetBBox().GetWidth() > layoutBox.GetWidth() && !(null == overflowX || OverflowPropertyValue
                            .FIT.Equals(overflowX))) {
                            occupiedArea.GetBBox().SetWidth(layoutBox.GetWidth());
                        }
                    }
                    firstLineInBox = false;
                    layoutBox.SetHeight(processedRenderer.GetOccupiedArea().GetBBox().GetY() - layoutBox.GetY());
                    lines.Add(processedRenderer);
                    anythingPlaced = true;
                    currentRenderer = (LineRenderer)result.GetOverflowRenderer();
                    previousDescent = processedRenderer.GetMaxDescent();
                }
            }
            float moveDown = lastLineBottomLeadingIndent;
            if ((null == overflowY || OverflowPropertyValue.FIT.Equals(overflowY)) && moveDown > occupiedArea.GetBBox(
                ).GetY() - layoutBox.GetY()) {
                moveDown = occupiedArea.GetBBox().GetY() - layoutBox.GetY();
            }
            occupiedArea.GetBBox().MoveDown(moveDown);
            occupiedArea.GetBBox().SetHeight(occupiedArea.GetBBox().GetHeight() + moveDown);
            float overflowPartHeight = GetOverflowPartHeight(overflowY, layoutBox);
            if (marginsCollapsingEnabled) {
                if (childRenderers.Count > 0 && notAllKidsAreFloats) {
                    marginsCollapseHandler.EndChildMarginsHandling(layoutBox);
                }
                marginsCollapseHandler.EndMarginsCollapse(layoutBox);
            }
            if (FloatingHelper.IsRendererFloating(this, floatPropertyValue)) {
                FloatingHelper.IncludeChildFloatsInOccupiedArea(floatRendererAreas, this);
            }
            iText.Layout.Renderer.ParagraphRenderer overflowRenderer = null;
            float? blockMinHeight = RetrieveMinHeight();
            if (null != blockMinHeight && blockMinHeight > occupiedArea.GetBBox().GetHeight()) {
                float blockBottom = occupiedArea.GetBBox().GetBottom() - ((float)blockMinHeight - occupiedArea.GetBBox().GetHeight
                    ());
                if (blockBottom >= layoutContext.GetArea().GetBBox().GetBottom() || (null != overflowY && !OverflowPropertyValue
                    .FIT.Equals(overflowY))) {
                    occupiedArea.GetBBox().SetY(blockBottom).SetHeight((float)blockMinHeight);
                }
                else {
                    occupiedArea.GetBBox().IncreaseHeight(occupiedArea.GetBBox().GetBottom() - layoutContext.GetArea().GetBBox
                        ().GetBottom()).SetY(layoutContext.GetArea().GetBBox().GetBottom());
                    overflowRenderer = CreateOverflowRenderer(parent);
                    overflowRenderer.UpdateMinHeight(UnitValue.CreatePointValue((float)blockMinHeight - occupiedArea.GetBBox()
                        .GetHeight()));
                    if (HasProperty(Property.HEIGHT)) {
                        overflowRenderer.UpdateHeight(UnitValue.CreatePointValue((float)RetrieveHeight() - occupiedArea.GetBBox().
                            GetHeight()));
                    }
                }
                ApplyVerticalAlignment();
            }
            CorrectPositionedLayout(layoutBox);
            ApplyPaddings(occupiedArea.GetBBox(), paddings, true);
            ApplyBorderBox(occupiedArea.GetBBox(), borders, true);
            ApplyMargins(occupiedArea.GetBBox(), true);
            ApplyAbsolutePositionIfNeeded(layoutContext);
            if (rotation != null) {
                ApplyRotationLayout(layoutContext.GetArea().GetBBox().Clone());
                if (IsNotFittingLayoutArea(layoutContext.GetArea())) {
                    if (IsNotFittingWidth(layoutContext.GetArea()) && !IsNotFittingHeight(layoutContext.GetArea())) {
                        LoggerFactory.GetLogger(GetType()).Warn(MessageFormatUtil.Format(iText.IO.LogMessageConstant.ELEMENT_DOES_NOT_FIT_AREA
                            , "It fits by height so it will be forced placed"));
                    }
                    else {
                        if (!true.Equals(GetPropertyAsBoolean(Property.FORCED_PLACEMENT))) {
                            return new MinMaxWidthLayoutResult(LayoutResult.NOTHING, null, null, this, this);
                        }
                    }
                }
            }
            if (wasHeightClipped) {
                occupiedArea.GetBBox().MoveUp(overflowPartHeight).DecreaseHeight(overflowPartHeight);
            }
            FloatingHelper.RemoveFloatsAboveRendererBottom(floatRendererAreas, this);
            LayoutArea editedArea_1 = FloatingHelper.AdjustResultOccupiedAreaForFloatAndClear(this, layoutContext.GetFloatRendererAreas
                (), layoutContext.GetArea().GetBBox(), clearHeightCorrection, marginsCollapsingEnabled);
            if (null == overflowRenderer) {
                return new MinMaxWidthLayoutResult(LayoutResult.FULL, editedArea_1, null, null, null).SetMinMaxWidth(minMaxWidth
                    );
            }
            else {
                return new MinMaxWidthLayoutResult(LayoutResult.PARTIAL, editedArea_1, this, overflowRenderer, null).SetMinMaxWidth
                    (minMaxWidth);
            }
        }

        /// <summary><inheritDoc/></summary>
        public override IRenderer GetNextRenderer() {
            return new iText.Layout.Renderer.ParagraphRenderer((Paragraph)modelElement);
        }

        /// <summary><inheritDoc/></summary>
        public override T1 GetDefaultProperty<T1>(int property) {
            if ((property == Property.MARGIN_TOP || property == Property.MARGIN_BOTTOM) && parent is CellRenderer) {
                return (T1)(Object)0f;
            }
            return base.GetDefaultProperty<T1>(property);
        }

        /// <summary><inheritDoc/></summary>
        public override String ToString() {
            StringBuilder sb = new StringBuilder();
            if (lines != null && lines.Count > 0) {
                for (int i = 0; i < lines.Count; i++) {
                    if (i > 0) {
                        sb.Append("\n");
                    }
                    sb.Append(lines[i].ToString());
                }
            }
            else {
                foreach (IRenderer renderer in childRenderers) {
                    sb.Append(renderer.ToString());
                }
            }
            return sb.ToString();
        }

        /// <summary><inheritDoc/></summary>
        public override void DrawChildren(DrawContext drawContext) {
            if (lines != null) {
                foreach (LineRenderer line in lines) {
                    line.Draw(drawContext);
                }
            }
        }

        /// <summary><inheritDoc/></summary>
        public override void Move(float dxRight, float dyUp) {
            occupiedArea.GetBBox().MoveRight(dxRight);
            occupiedArea.GetBBox().MoveUp(dyUp);
            if (null != lines) {
                foreach (LineRenderer line in lines) {
                    line.Move(dxRight, dyUp);
                }
            }
        }

        /// <summary>
        /// Gets the lines which are the result of the
        /// <see cref="Layout(iText.Layout.Layout.LayoutContext)"/>
        /// .
        /// </summary>
        /// <returns>paragraph lines, or <code>null</code> if layout hasn't been called yet</returns>
        public virtual IList<LineRenderer> GetLines() {
            return lines;
        }

        protected internal override float? GetFirstYLineRecursively() {
            if (lines == null || lines.Count == 0) {
                return null;
            }
            return lines[0].GetFirstYLineRecursively();
        }

        protected internal override float? GetLastYLineRecursively() {
            if (lines == null || lines.Count == 0) {
                return null;
            }
            for (int i = lines.Count - 1; i >= 0; i--) {
                float? yLine = lines[i].GetLastYLineRecursively();
                if (yLine != null) {
                    return yLine;
                }
            }
            return null;
        }

        [Obsolete]
        protected internal virtual iText.Layout.Renderer.ParagraphRenderer CreateOverflowRenderer() {
            return (iText.Layout.Renderer.ParagraphRenderer)GetNextRenderer();
        }

        [Obsolete]
        protected internal virtual iText.Layout.Renderer.ParagraphRenderer CreateSplitRenderer() {
            return (iText.Layout.Renderer.ParagraphRenderer)GetNextRenderer();
        }

        protected internal virtual iText.Layout.Renderer.ParagraphRenderer CreateOverflowRenderer(IRenderer parent
            ) {
            iText.Layout.Renderer.ParagraphRenderer overflowRenderer = CreateOverflowRenderer();
            overflowRenderer.parent = parent;
            FixOverflowRenderer(overflowRenderer);
            return overflowRenderer;
        }

        protected internal virtual iText.Layout.Renderer.ParagraphRenderer CreateSplitRenderer(IRenderer parent) {
            iText.Layout.Renderer.ParagraphRenderer splitRenderer = CreateSplitRenderer();
            splitRenderer.parent = parent;
            splitRenderer.properties = new Dictionary<int, Object>(properties);
            return splitRenderer;
        }

        protected internal override MinMaxWidth GetMinMaxWidth(float availableWidth) {
            MinMaxWidth minMaxWidth = new MinMaxWidth(0, availableWidth);
            float? rotation = this.GetPropertyAsFloat(Property.ROTATION_ANGLE);
            if (!SetMinMaxWidthBasedOnFixedWidth(minMaxWidth)) {
                float? minWidth = HasAbsoluteUnitValue(Property.MIN_WIDTH) ? RetrieveMinWidth(0) : null;
                float? maxWidth = HasAbsoluteUnitValue(Property.MAX_WIDTH) ? RetrieveMaxWidth(0) : null;
                if (minWidth == null || maxWidth == null) {
                    bool restoreRotation = HasOwnProperty(Property.ROTATION_ANGLE);
                    SetProperty(Property.ROTATION_ANGLE, null);
                    MinMaxWidthLayoutResult result = (MinMaxWidthLayoutResult)Layout(new LayoutContext(new LayoutArea(1, new Rectangle
                        (availableWidth, AbstractRenderer.INF))));
                    if (restoreRotation) {
                        SetProperty(Property.ROTATION_ANGLE, rotation);
                    }
                    else {
                        DeleteOwnProperty(Property.ROTATION_ANGLE);
                    }
                    minMaxWidth = result.GetNotNullMinMaxWidth(availableWidth);
                }
                if (minWidth != null) {
                    minMaxWidth.SetChildrenMinWidth((float)minWidth);
                }
                if (maxWidth != null) {
                    minMaxWidth.SetChildrenMaxWidth((float)maxWidth);
                }
                if (minMaxWidth.GetChildrenMinWidth() > minMaxWidth.GetChildrenMaxWidth()) {
                    minMaxWidth.SetChildrenMaxWidth(minMaxWidth.GetChildrenMaxWidth());
                }
            }
            else {
                minMaxWidth.SetAdditionalWidth(CalculateAdditionalWidth(this));
            }
            return rotation != null ? RotationUtils.CountRotationMinMaxWidth(minMaxWidth, this) : minMaxWidth;
        }

        protected internal virtual iText.Layout.Renderer.ParagraphRenderer[] Split() {
            iText.Layout.Renderer.ParagraphRenderer splitRenderer = CreateSplitRenderer(parent);
            splitRenderer.occupiedArea = occupiedArea;
            splitRenderer.isLastRendererForModelElement = false;
            iText.Layout.Renderer.ParagraphRenderer overflowRenderer = CreateOverflowRenderer(parent);
            return new iText.Layout.Renderer.ParagraphRenderer[] { splitRenderer, overflowRenderer };
        }

        private void FixOverflowRenderer(iText.Layout.Renderer.ParagraphRenderer overflowRenderer) {
            // Reset first line indent in case of overflow.
            float firstLineIndent = (float)overflowRenderer.GetPropertyAsFloat(Property.FIRST_LINE_INDENT);
            if (firstLineIndent != 0) {
                overflowRenderer.SetProperty(Property.FIRST_LINE_INDENT, 0);
            }
        }
    }
}
