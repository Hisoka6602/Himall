define("echarts/chart/pie", ["require", "../component/base", "./base", "zrender/shape/Text", "zrender/shape/Ring", "zrender/shape/Circle", "zrender/shape/Sector", "zrender/shape/BrokenLine", "../config", "../util/ecData", "zrender/tool/util", "zrender/tool/math", "zrender/tool/color", "../chart"],
function (e) {
    function t(e, t, a, o, s) {
        i.call(this, e, t, a, o, s),
        n.call(this);
        var r = this;
        r.shapeHandler.onmouseover = function (e) {
            var t = e.target,
            i = d.get(t, "seriesIndex"),
            n = d.get(t, "dataIndex"),
            a = d.get(t, "special"),
            o = [t.style.x, t.style.y],
            s = t.style.startAngle,
            l = t.style.endAngle,
            h = ((l + s) / 2 + 360) % 360,
            m = t.highlightStyle.color,
            c = r.getLabel(i, n, a, o, h, m, !0);
            c && r.zr.addHoverShape(c);
            var p = r.getLabelLine(i, n, o, t.style.r0, t.style.r, h, m, !0);
            p && r.zr.addHoverShape(p)
        },
        this.refresh(o)
    }
    var i = e("../component/base"),
    n = e("./base"),
    a = e("zrender/shape/Text"),
    o = e("zrender/shape/Ring"),
    s = e("zrender/shape/Circle"),
    r = e("zrender/shape/Sector"),
    l = e("zrender/shape/BrokenLine"),
    h = e("../config"),
    d = e("../util/ecData"),
    m = e("zrender/tool/util"),
    c = e("zrender/tool/math"),
    p = e("zrender/tool/color"),
    customValue = 100;
    return t.prototype = {
        type: h.CHART_TYPE_PIE,
        _buildShape: function () {

            var e = this.series,
            t = this.component.legend;
            customValue = e[0].pieNumber ? e[0].pieNumber : 100;
            this.selectedMap = {},
            this._selected = {};
            var i, n, a;
            this._selectedMode = !1;
            for (var r, l = 0,
            m = e.length; m > l; l++) if (e[l].type === h.CHART_TYPE_PIE) {
                if (e[l] = this.reformOption(e[l]), this.legendHoverLink = e[l].legendHoverLink || this.legendHoverLink, r = e[l].name || "", this.selectedMap[r] = t ? t.isSelected(r) : !0, !this.selectedMap[r]) continue;
                i = this.parseCenter(this.zr, e[l].center),
                n = this.parseRadius(this.zr, e[l].radius),
                this._selectedMode = this._selectedMode || e[l].selectedMode,
                this._selected[l] = [],
                this.deepQuery([e[l], this.option], "calculable") && (a = {
                    zlevel: this._zlevelBase,
                    hoverable: !1,
                    style: {
                        x: i[0],
                        y: i[1],
                        r0: n[0] <= 10 ? 0 : n[0] - 10,
                        r: n[1] + 10,
                        brushType: "stroke",
                        lineWidth: 1,
                        strokeColor: e[l].calculableHolderColor || this.ecTheme.calculableHolderColor
                    }
                },
                d.pack(a, e[l], l, void 0, -1), this.setCalculable(a), a = n[0] <= 10 ? new s(a) : new o(a), this.shapeList.push(a)),
                this._buildSinglePie(l),
                this.buildMark(l)
            }
            this.addShapeList()
        },
        _buildSinglePie: function (e) {

            for (var t, i = this.series,
            n = i[e], a = n.data, o = this.component.legend, s = 0, r = 0, l = 0, h = Number.NEGATIVE_INFINITY, d = [], m = 0, c = a.length; c > m; m++) t = a[m].name,
            this.selectedMap[t] = o ? o.isSelected(t) : !0,
            this.selectedMap[t] && !isNaN(a[m].value) && (0 !== +a[m].value ? s++ : r++, l += +a[m].value, h = Math.max(h, +a[m].value));
            if (0 !== l) {
                //customValue ??????100??????????????????????
                for (var p, u, V, U, g, y, f = customValue,
                _ = n.clockWise,
                b = (n.startAngle.toFixed(2) - 0 + 360) % 360, x = n.minAngle || .01, k = 360 - x * s - .01 * r, L = n.roseType, m = 0, c = a.length; c > m; m++) if (t = a[m].name, this.selectedMap[t] && !isNaN(a[m].value)) {
                    if (u = o ? o.getColor(t) : this.zr.getColor(m), f = a[m].value / l, p = "area" != L ? _ ? b - f * k - (0 !== f ? x : .01) : f * k + b + (0 !== f ? x : .01) : _ ? b - 360 / c : 360 / c + b, p = p.toFixed(2) - 0, f = (100 * f).toFixed(2), V = this.parseCenter(this.zr, n.center), U = this.parseRadius(this.zr, n.radius), g = +U[0], y = +U[1], "radius" === L ? y = a[m].value / h * (y - g) * .8 + .2 * (y - g) + g : "area" === L && (y = Math.sqrt(a[m].value / h) * (y - g) + g), _) {
                        var v;
                        v = b,
                        b = p,
                        p = v
                    }
                    this._buildItem(d, e, m, f, a[m].selected, V, g, y, b, p, u),
                    _ || (b = p)
                }
                this._autoLabelLayout(d, V, y);
                for (var m = 0,
                c = d.length; c > m; m++) this.shapeList.push(d[m]);
                d = null
            }
        },
        _buildItem: function (e, t, i, n, a, o, s, r, l, h, m) {
            var c = this.series,
            p = ((h + l) / 2 + 360) % 360,
            u = this.getSector(t, i, n, a, o, s, r, l, h, m);
            d.pack(u, c[t], t, c[t].data[i], i, c[t].data[i].name, n),
            e.push(u);
            var V = this.getLabel(t, i, n, o, p, m, !1),
            U = this.getLabelLine(t, i, o, s, r, p, m, !1);
            U && (d.pack(U, c[t], t, c[t].data[i], i, c[t].data[i].name, n), e.push(U)),
            V && (d.pack(V, c[t], t, c[t].data[i], i, c[t].data[i].name, n), V._labelLine = U, e.push(V))
        },
        getSector: function (e, t, i, n, a, o, s, l, h, d) {
            var m = this.series,
            u = m[e],
            V = u.data[t],
            U = [V, u],
            g = this.deepMerge(U, "itemStyle.normal") || {},
            y = this.deepMerge(U, "itemStyle.emphasis") || {},
            f = this.getItemStyleColor(g.color, e, t, V) || d,
            _ = this.getItemStyleColor(y.color, e, t, V) || ("string" == typeof f ? p.lift(f, -.2) : f),
            b = {
                zlevel: this._zlevelBase,
                clickable: this.deepQuery(U, "clickable"),
                style: {
                    x: a[0],
                    y: a[1],
                    r0: o,
                    r: s,
                    startAngle: l,
                    endAngle: h,
                    brushType: "both",
                    color: f,
                    lineWidth: g.borderWidth,
                    strokeColor: g.borderColor,
                    lineJoin: "round"
                },
                highlightStyle: {
                    color: _,
                    lineWidth: y.borderWidth,
                    strokeColor: y.borderColor,
                    lineJoin: "round"
                },
                _seriesIndex: e,
                _dataIndex: t
            };
            if (n) {
                var x = ((b.style.startAngle + b.style.endAngle) / 2).toFixed(2) - 0;
                b.style._hasSelected = !0,
                b.style._x = b.style.x,
                b.style._y = b.style.y;
                var k = this.query(u, "selectedOffset");
                b.style.x += c.cos(x, !0) * k,
                b.style.y -= c.sin(x, !0) * k,
                this._selected[e][t] = !0
            } else this._selected[e][t] = !1;
            return this._selectedMode && (b.onclick = this.shapeHandler.onclick),
            this.deepQuery([V, u, this.option], "calculable") && (this.setCalculable(b), b.draggable = !0),
            (this._needLabel(u, V, !0) || this._needLabelLine(u, V, !0)) && (b.onmouseover = this.shapeHandler.onmouseover),
            b = new r(b)
        },
        getLabel: function (e, t, i, n, o, s, r) {
            var l = this.series,
            h = l[e],
            d = h.data[t];
            if (this._needLabel(h, d, r)) {
                var p, u, V, U = r ? "emphasis" : "normal",
                g = m.merge(m.clone(d.itemStyle) || {},
                h.itemStyle),
                y = g[U].label,
                f = y.textStyle || {},
                _ = n[0],
                b = n[1],
                x = this.parseRadius(this.zr, h.radius),
                k = "middle";
                y.position = y.position || g.normal.label.position,
                "center" === y.position ? (p = _, u = b, V = "center") : "inner" === y.position || "inside" === y.position ? (x = (x[0] + x[1]) / 2, p = Math.round(_ + x * c.cos(o, !0)), u = Math.round(b - x * c.sin(o, !0)), s = "#fff", V = "center") : (x = x[1] - -g[U].labelLine.length, p = Math.round(_ + x * c.cos(o, !0)), u = Math.round(b - x * c.sin(o, !0)), V = o >= 90 && 270 >= o ? "right" : "left"),
                "center" != y.position && "inner" != y.position && "inside" != y.position && (p += "left" === V ? 20 : -20),
                d.__labelX = p - ("left" === V ? 5 : -5),
                d.__labelY = u;
                var L = new a({
                    zlevel: this._zlevelBase + 1,
                    hoverable: !1,
                    style: {
                        x: p,
                        y: u,
                        color: f.color || s,
                        text: this.getLabelText(e, t, i, U),
                        textAlign: f.align || V,
                        textBaseline: f.baseline || k,
                        textFont: this.getFont(f)
                    },
                    highlightStyle: {
                        brushType: "fill"
                    }
                });
                return L._radius = x,
                L._labelPosition = y.position || "outer",
                L._rect = L.getRect(L.style),
                L._seriesIndex = e,
                L._dataIndex = t,
                L
            }
        },
        getLabelText: function (e, t, i, n) {
            var a = this.series,
            o = a[e],
            s = o.data[t],
            r = this.deepQuery([s, o], "itemStyle." + n + ".label.formatter");
            return r ? "function" == typeof r ? r.call(this.myChart, o.name, s.name, s.value, i) : "string" == typeof r ? (r = r.replace("{a}", "{a0}").replace("{b}", "{b0}").replace("{c}", "{c0}").replace("{d}", "{d0}"), r = r.replace("{a0}", o.name).replace("{b0}", s.name).replace("{c0}", s.value).replace("{d0}", i)) : void 0 : s.name
        },
        getLabelLine: function (e, t, i, n, a, o, s, r) {
            var h = this.series,
            d = h[e],
            p = d.data[t];
            if (this._needLabelLine(d, p, r)) {
                var u = r ? "emphasis" : "normal",
                V = m.merge(m.clone(p.itemStyle) || {},
                d.itemStyle),
                U = V[u].labelLine,
                g = U.lineStyle || {},
                y = i[0],
                f = i[1],
                _ = a,
                b = this.parseRadius(this.zr, d.radius)[1] - -U.length,
                x = c.cos(o, !0),
                k = c.sin(o, !0);
                return new l({
                    zlevel: this._zlevelBase + 1,
                    hoverable: !1,
                    style: {
                        pointList: [[y + _ * x, f - _ * k], [y + b * x, f - b * k], [p.__labelX, p.__labelY]],
                        strokeColor: g.color || s,
                        lineType: g.type,
                        lineWidth: g.width
                    },
                    _seriesIndex: e,
                    _dataIndex: t
                })
            }
        },
        _needLabel: function (e, t, i) {
            return this.deepQuery([t, e], "itemStyle." + (i ? "emphasis" : "normal") + ".label.show")
        },
        _needLabelLine: function (e, t, i) {
            return this.deepQuery([t, e], "itemStyle." + (i ? "emphasis" : "normal") + ".labelLine.show")
        },
        _autoLabelLayout: function (e, t, i) {
            for (var n = [], a = [], o = 0, s = e.length; s > o; o++) ("outer" === e[o]._labelPosition || "outside" === e[o]._labelPosition) && (e[o]._rect._y = e[o]._rect.y, e[o]._rect.x < t[0] ? n.push(e[o]) : a.push(e[o]));
            this._layoutCalculate(n, t, i, -1),
            this._layoutCalculate(a, t, i, 1)
        },
        _layoutCalculate: function (e, t, i, n) {
            function a(t, i, n) {
                for (var a = t; i > a; a++) if (e[a]._rect.y += n, e[a].style.y += n, e[a]._labelLine && (e[a]._labelLine.style.pointList[1][1] += n, e[a]._labelLine.style.pointList[2][1] += n), a > t && i > a + 1 && e[a + 1]._rect.y > e[a]._rect.y + e[a]._rect.height) return void o(a, n / 2);
                o(i - 1, n / 2)
            }
            function o(t, i) {
                for (var n = t; n >= 0 && (e[n]._rect.y -= i, e[n].style.y -= i, e[n]._labelLine && (e[n]._labelLine.style.pointList[1][1] -= i, e[n]._labelLine.style.pointList[2][1] -= i), !(n > 0 && e[n]._rect.y > e[n - 1]._rect.y + e[n - 1]._rect.height)) ; n--);
            }
            function s(e, t, i, n, a) {
                for (var o, s, r, l = i[0], h = i[1], d = a > 0 ? t ? Number.MAX_VALUE : 0 : t ? Number.MAX_VALUE : 0, m = 0, c = e.length; c > m; m++) s = Math.abs(e[m]._rect.y - h),
                r = e[m]._radius - n,
                o = n + r > s ? Math.sqrt((n + r + 20) * (n + r + 20) - Math.pow(e[m]._rect.y - h, 2)) : Math.abs(e[m]._rect.x + (a > 0 ? 0 : e[m]._rect.width) - l),
                t && o >= d && (o = d - 10),
                !t && d >= o && (o = d + 10),
                e[m]._rect.x = e[m].style.x = l + o * a,
                e[m]._labelLine.style.pointList[2][0] = l + (o - 5) * a,
                e[m]._labelLine.style.pointList[1][0] = l + (o - 20) * a,
                d = o
            }
            e.sort(function (e, t) {
                return e._rect.y - t._rect.y
            });
            for (var r, l = 0,
            h = e.length,
            d = [], m = [], c = 0; h > c; c++) r = e[c]._rect.y - l,
            0 > r && a(c, h, -r, n),
            l = e[c]._rect.y + e[c]._rect.height;
            this.zr.getHeight() - l < 0 && o(h - 1, l - this.zr.getHeight());
            for (var c = 0; h > c; c++) e[c]._rect.y >= t[1] ? m.push(e[c]) : d.push(e[c]);
            s(m, !0, t, i, n),
            s(d, !1, t, i, n)
        },
        reformOption: function (e) {
            var t = m.merge;
            return e = t(e || {},
            this.ecTheme.pie),
            e.itemStyle.normal.label.textStyle = t(e.itemStyle.normal.label.textStyle || {},
            this.ecTheme.textStyle),
            e.itemStyle.emphasis.label.textStyle = t(e.itemStyle.emphasis.label.textStyle || {},
            this.ecTheme.textStyle),
            e
        },
        refresh: function (e) {
            e && (this.option = e, this.series = e.series),
            this.backupShapeList(),
            this._buildShape()
        },
        addDataAnimation: function (e) {
            for (var t = this.series,
            i = {},
            n = 0,
            a = e.length; a > n; n++) i[e[n][0]] = e[n];
            var o = {},
            s = {},
            r = {},
            l = this.shapeList;
            this.shapeList = [];
            for (var d, m, c, p = {},
            n = 0,
            a = e.length; a > n; n++) d = e[n][0],
            m = e[n][2],
            c = e[n][3],
            t[d] && t[d].type === h.CHART_TYPE_PIE && (m ? (c || (o[d + "_" + t[d].data.length] = "delete"), p[d] = 1) : c ? p[d] = 0 : (o[d + "_-1"] = "delete", p[d] = -1), this._buildSinglePie(d));
            for (var u, V, n = 0,
            a = this.shapeList.length; a > n; n++) switch (d = this.shapeList[n]._seriesIndex, u = this.shapeList[n]._dataIndex, V = d + "_" + u, this.shapeList[n].type) {
                case "sector":
                    o[V] = this.shapeList[n];
                    break;
                case "text":
                    s[V] = this.shapeList[n];
                    break;
                case "broken-line":
                    r[V] = this.shapeList[n]
            }
            this.shapeList = [];
            for (var U, n = 0,
            a = l.length; a > n; n++) if (d = l[n]._seriesIndex, i[d]) {
                if (u = l[n]._dataIndex + p[d], V = d + "_" + u, U = o[V], !U) continue;
                if ("sector" === l[n].type) "delete" != U ? this.zr.animate(l[n].id, "style").when(400, {
                    startAngle: U.style.startAngle,
                    endAngle: U.style.endAngle
                }).start() : this.zr.animate(l[n].id, "style").when(400, p[d] < 0 ? {
                    startAngle: l[n].style.startAngle
                } : {
                    endAngle: l[n].style.endAngle
                }).start();
                else if ("text" === l[n].type || "broken-line" === l[n].type) if ("delete" === U) this.zr.delShape(l[n].id);
                else switch (l[n].type) {
                    case "text":
                        U = s[V],
                        this.zr.animate(l[n].id, "style").when(400, {
                            x: U.style.x,
                            y: U.style.y
                        }).start();
                        break;
                    case "broken-line":
                        U = r[V],
                        this.zr.animate(l[n].id, "style").when(400, {
                            pointList: U.style.pointList
                        }).start()
                }
            }
            this.shapeList = l
        },
        onclick: function (e) {
            var t = this.series;
            if (this.isClick && e.target) {
                this.isClick = !1;
                for (var i, n = e.target,
                a = n.style,
                o = d.get(n, "seriesIndex"), s = d.get(n, "dataIndex"), r = 0, l = this.shapeList.length; l > r; r++) if (this.shapeList[r].id === n.id) {
                    if (o = d.get(n, "seriesIndex"), s = d.get(n, "dataIndex"), a._hasSelected) n.style.x = n.style._x,
                    n.style.y = n.style._y,
                    n.style._hasSelected = !1,
                    this._selected[o][s] = !1;
                    else {
                        var m = ((a.startAngle + a.endAngle) / 2).toFixed(2) - 0;
                        n.style._hasSelected = !0,
                        this._selected[o][s] = !0,
                        n.style._x = n.style.x,
                        n.style._y = n.style.y,
                        i = this.query(t[o], "selectedOffset"),
                        n.style.x += c.cos(m, !0) * i,
                        n.style.y -= c.sin(m, !0) * i
                    }
                    this.zr.modShape(n.id, n)
                } else this.shapeList[r].style._hasSelected && "single" === this._selectedMode && (o = d.get(this.shapeList[r], "seriesIndex"), s = d.get(this.shapeList[r], "dataIndex"), this.shapeList[r].style.x = this.shapeList[r].style._x, this.shapeList[r].style.y = this.shapeList[r].style._y, this.shapeList[r].style._hasSelected = !1, this._selected[o][s] = !1, this.zr.modShape(this.shapeList[r].id, this.shapeList[r]));
                this.messageCenter.dispatch(h.EVENT.PIE_SELECTED, e.event, {
                    selected: this._selected,
                    target: d.get(n, "name")
                },
                this.myChart),
                this.zr.refresh()
            }
        }
    },
    m.inherits(t, n),
    m.inherits(t, i),
    e("../chart").define("pie", t),
    t
});
