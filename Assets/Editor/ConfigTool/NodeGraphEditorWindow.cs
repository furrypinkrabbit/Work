using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NodeConfigTool
{
    public class NodeGraphEditorWindow : EditorWindow
    {
        #region 样式常量配置
        private const float NodeWidth = 200f;
        private const float NodeTitleHeight = 24f;
        private const float NodePropertyHeight = 20f;
        private const float InspectorWidth = 300f;
        private const float TopButtonHeight = 30f;
        private readonly Color NodeNormalColor = new Color(0.2f, 0.2f, 0.2f);
        private readonly Color NodeSelectedColor = new Color(0.3f, 0.3f, 0.15f);
        private readonly Color NodeRootColor = new Color(0.1f, 0.3f, 0.5f);
        private readonly Color LineColor = Color.red;
        private readonly Color TempLineColor = Color.yellow;
        #endregion

        #region 运行时状态
        private NodeConfigData configData;
        private NodeData selectedNode;
        private Vector2 canvasOffset = Vector2.zero;

        // 画布拖拽
        private bool isDraggingCanvas = false;
        private Vector2 dragStartMousePos;
        private Vector2 dragStartCanvasOffset;

        // 连线状态
        private bool isConnecting = false;
        private NodeData connectingFromNode;
        private Vector2 connectingMousePos;

        // 节点拖拽
        private bool isDraggingNode = false;
        private Vector2 dragNodeStartOffset;
        #endregion

        #region 窗口入口与生命周期
        /// <summary>
        /// 打开新配置窗口
        /// </summary>
        public static void OpenNewConfigWindow()
        {
            var window = GetWindow<NodeGraphEditorWindow>("节点可视化配置工具");
            window.minSize = new Vector2(1200, 800);
            window.InitNewConfig();
            window.Show();
        }

        /// <summary>
        /// 打开已有配置的编辑窗口
        /// </summary>
        public static void OpenEditConfigWindow(string filePath)
        {
            var window = GetWindow<NodeGraphEditorWindow>("节点可视化配置工具");
            window.minSize = new Vector2(1200, 800);
            window.LoadConfigFromFile(filePath);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true; // 接收鼠标移动事件，用于连线跟随
        }

        private void OnDisable()
        {
            // 窗口关闭时清理状态
            configData = null;
            selectedNode = null;
            isConnecting = false;
            connectingFromNode = null;
        }
        #endregion

        #region 初始化与加载
        /// <summary>
        /// 初始化新配置，自动创建root根节点
        /// </summary>
        public void InitNewConfig()
        {
            configData = new NodeConfigData();
            // 根节点默认放在画布中间
            var rootNode = new NodeData
            {
                NodeName = "root",
                Position = new Vector2(400, 300)
            };
            configData.Nodes.Add(rootNode);
            selectedNode = null;
            canvasOffset = Vector2.zero;
        }

        /// <summary>
        /// 从XML文件加载配置
        /// </summary>
        public void LoadConfigFromFile(string filePath)
        {
            var loadedData = XMLConfigSerializer.LoadConfigFromXML(filePath);
            if (loadedData != null)
            {
                configData = loadedData;
                selectedNode = null;
                canvasOffset = Vector2.zero;
                Repaint();
            }
        }
        #endregion

        #region 核心GUI绘制
        private void OnGUI()
        {
            if (configData == null) return;

            // 1. 绘制顶部按钮栏
            DrawTopButtonBar();

            // 2. 绘制画布背景与网格
            DrawCanvasBackground();

            // 3. 处理输入事件
            ProcessInputEvents();

            // 4. 绘制连线（先画线再画节点，保证节点在最上层）
            DrawAllConnections();

            // 5. 绘制正在创建的临时连线
            if (isConnecting)
            {
                DrawTempConnectionLine();
            }

            // 6. 绘制所有节点
            DrawAllNodes();

            // 7. 绘制右侧Inspector属性面板
            DrawInspectorPanel();

            // 8. 自动重绘
            if (Event.current.type == EventType.MouseMove || isConnecting || isDraggingNode || isDraggingCanvas)
            {
                Repaint();
            }
        }

        /// <summary>
        /// 绘制右上角的保存、清空、退出按钮
        /// </summary>
        private void DrawTopButtonBar()
        {
            GUILayout.BeginArea(new Rect(position.width - 320, 5, 300, TopButtonHeight));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("保存配置", GUILayout.Height(TopButtonHeight)))
            {
                SaveConfig();
            }

            if (GUILayout.Button("清空面板", GUILayout.Height(TopButtonHeight)))
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有节点吗？根节点会保留。", "确定", "取消"))
                {
                    ClearPanel();
                }
            }

            if (GUILayout.Button("退出", GUILayout.Height(TopButtonHeight)))
            {
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制画布背景与网格线
        /// </summary>
        private void DrawCanvasBackground()
        {
            Rect canvasRect = new Rect(0, TopButtonHeight + 5, position.width - InspectorWidth, position.height - TopButtonHeight - 5);
            EditorGUI.DrawRect(canvasRect, new Color(0.15f, 0.15f, 0.15f));

            // 绘制网格
            DrawGrid(canvasRect, 20, new Color(0.1f, 0.1f, 0.1f));
            DrawGrid(canvasRect, 100, new Color(0.08f, 0.08f, 0.08f));
        }

        private void DrawGrid(Rect canvasRect, float gridSpacing, Color gridColor)
        {
            Handles.color = gridColor;
            // 横线
            float startY = canvasRect.y + canvasOffset.y % gridSpacing;
            for (float y = startY; y < canvasRect.yMax; y += gridSpacing)
            {
                Handles.DrawLine(new Vector3(canvasRect.xMin, y), new Vector3(canvasRect.xMax, y));
            }
            // 竖线
            float startX = canvasRect.x + canvasOffset.x % gridSpacing;
            for (float x = startX; x < canvasRect.xMax; x += gridSpacing)
            {
                Handles.DrawLine(new Vector3(x, canvasRect.yMin), new Vector3(x, canvasRect.yMax));
            }
        }

        /// <summary>
        /// 绘制所有节点之间的贝塞尔连线
        /// </summary>
        private void DrawAllConnections()
        {
            Handles.BeginGUI();
            foreach (var fromNode in configData.Nodes)
            {
                foreach (var nextGuid in fromNode.NextNodeGuids)
                {
                    var toNode = configData.Nodes.Find(n => n.Guid == nextGuid);
                    if (toNode == null) continue;

                    Vector2 startPos = GetNodeConnectionStartPos(fromNode);
                    Vector2 endPos = GetNodeConnectionEndPos(toNode);
                    DrawBezierLine(startPos, endPos, LineColor, 2f);
                }
            }
            Handles.EndGUI();
        }

        /// <summary>
        /// 绘制跟随鼠标的临时连线
        /// </summary>
        private void DrawTempConnectionLine()
        {
            if (connectingFromNode == null) return;

            Handles.BeginGUI();
            Vector2 startPos = GetNodeConnectionStartPos(connectingFromNode);
            DrawBezierLine(startPos, connectingMousePos, TempLineColor, 2f);
            Handles.EndGUI();
        }

        /// <summary>
        /// 绘制贝塞尔曲线
        /// </summary>
        private void DrawBezierLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 startTangent = start + Vector2.right * 50f;
            Vector2 endTangent = end + Vector2.left * 50f;
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, width);
        }

        /// <summary>
        /// 获取节点连线起点（节点右侧中点）
        /// </summary>
        private Vector2 GetNodeConnectionStartPos(NodeData node)
        {
            return new Vector2(
                node.Position.x + NodeWidth + canvasOffset.x,
                node.Position.y + NodeTitleHeight / 2 + canvasOffset.y
            );
        }

        /// <summary>
        /// 获取节点连线终点（节点左侧中点）
        /// </summary>
        private Vector2 GetNodeConnectionEndPos(NodeData node)
        {
            return new Vector2(
                node.Position.x + canvasOffset.x,
                node.Position.y + NodeTitleHeight / 2 + canvasOffset.y
            );
        }

        /// <summary>
        /// 计算节点的总高度（根据属性数量自适应）
        /// </summary>
        private float GetNodeHeight(NodeData node)
        {
            return NodeTitleHeight + node.Properties.Count * NodePropertyHeight;
        }

        /// <summary>
        /// 获取节点的屏幕矩形（包含画布偏移）
        /// </summary>
        private Rect GetNodeRect(NodeData node)
        {
            float height = GetNodeHeight(node);
            return new Rect(
                node.Position.x + canvasOffset.x,
                node.Position.y + canvasOffset.y,
                NodeWidth,
                height
            );
        }

        /// <summary>
        /// 批量绘制所有节点
        /// </summary>
        private void DrawAllNodes()
        {
            foreach (var node in configData.Nodes)
            {
                DrawSingleNode(node);
            }
        }

        /// <summary>
        /// 绘制单个节点
        /// </summary>
        private void DrawSingleNode(NodeData node)
        {
            Rect nodeRect = GetNodeRect(node);

            // 节点背景
            Color nodeColor = node.IsRoot ? NodeRootColor : (selectedNode == node ? NodeSelectedColor : NodeNormalColor);
            EditorGUI.DrawRect(nodeRect, nodeColor);

            // 节点边框
            Handles.color = selectedNode == node ? Color.yellow : Color.black;
            Handles.DrawWireCube(nodeRect.center, new Vector3(nodeRect.width, nodeRect.height, 0));

            // 节点标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, NodeTitleHeight), node.NodeName, titleStyle);

            // 节点属性预览
            GUIStyle propStyle = new GUIStyle(EditorStyles.label);
            propStyle.normal.textColor = Color.white;
            propStyle.fontSize = 10;
            propStyle.padding = new RectOffset(5, 5, 2, 2);

            for (int i = 0; i < node.Properties.Count; i++)
            {
                var prop = node.Properties[i];
                float yPos = nodeRect.y + NodeTitleHeight + i * NodePropertyHeight;
                string displayText = $"{prop.PropertyType}: {prop.Value}";
                GUI.Label(new Rect(nodeRect.x, yPos, nodeRect.width, NodePropertyHeight), displayText, propStyle);
            }
        }

        /// <summary>
        /// 绘制右侧Inspector属性面板
        /// </summary>
        private void DrawInspectorPanel()
        {
            // 面板背景
            Rect inspectorRect = new Rect(position.width - InspectorWidth, 0, InspectorWidth, position.height);
            EditorGUI.DrawRect(inspectorRect, new Color(0.18f, 0.18f, 0.18f));

            // 面板内容
            GUILayout.BeginArea(new Rect(inspectorRect.x + 10, 10, InspectorWidth - 20, position.height - 20));
            GUILayout.Label("节点属性面板", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (selectedNode == null)
            {
                GUILayout.Label("请选择一个节点查看属性", EditorStyles.centeredGreyMiniLabel);
                GUILayout.EndArea();
                return;
            }

            // 节点名称编辑
            GUILayout.Label("节点名称", EditorStyles.label);
            using (new EditorGUI.DisabledScope(selectedNode.IsRoot)) // 根节点禁止改名
            {
                string newName = EditorGUILayout.TextField(selectedNode.NodeName);
                if (newName != selectedNode.NodeName && !selectedNode.IsRoot)
                {
                    selectedNode.NodeName = newName;
                    Repaint();
                }
            }
            EditorGUILayout.Space(10);

            // 属性列表
            GUILayout.Label("节点属性", EditorStyles.label);
            EditorGUILayout.Space(5);

            // 绘制每个属性的编辑项
            for (int i = 0; i < selectedNode.Properties.Count; i++)
            {
                var prop = selectedNode.Properties[i];
                EditorGUILayout.BeginHorizontal("Box");

                EditorGUILayout.LabelField(prop.PropertyType.ToString(), GUILayout.Width(60));
                prop.Value = EditorGUILayout.TextField(prop.Value);

                // 删除属性按钮
                if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(18)))
                {
                    selectedNode.Properties.RemoveAt(i);
                    Repaint();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // 添加属性按钮
            if (GUILayout.Button("添加属性", GUILayout.Height(25)))
            {
                ShowAddPropertyMenu();
            }

            GUILayout.EndArea();
        }
        #endregion

        #region 输入事件处理
        /// <summary>
        /// 处理所有鼠标、键盘事件
        /// </summary>
        private void ProcessInputEvents()
        {
            Event currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;

            // 检查鼠标是否在画布区域（排除Inspector和顶部按钮）
            bool isInCanvas = mousePos.x < position.width - InspectorWidth && mousePos.y > TopButtonHeight + 5;

            // 更新连线的鼠标位置
            if (isConnecting)
            {
                connectingMousePos = mousePos;
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    HandleMouseDown(currentEvent, mousePos, isInCanvas);
                    break;

                case EventType.MouseDrag:
                    HandleMouseDrag(currentEvent, mousePos, isInCanvas);
                    break;

                case EventType.MouseUp:
                    HandleMouseUp(currentEvent, mousePos, isInCanvas);
                    break;

                case EventType.ContextClick: // 右键点击
                    HandleRightClick(currentEvent, mousePos, isInCanvas);
                    currentEvent.Use();
                    break;

                case EventType.KeyDown:
                    HandleKeyDown(currentEvent);
                    break;
            }
        }

        private void HandleMouseDown(Event evt, Vector2 mousePos, bool isInCanvas)
        {
            if (!isInCanvas) return;

            // 左键按下
            if (evt.button == 0)
            {
                // 正在连线状态，处理连线点击
                if (isConnecting)
                {
                    HandleConnectingClick(mousePos);
                    evt.Use();
                    return;
                }

                // 检查是否点击到节点
                NodeData clickedNode = GetNodeAtPosition(mousePos);
                if (clickedNode != null)
                {
                    selectedNode = clickedNode;
                    isDraggingNode = true;
                    dragNodeStartOffset = new Vector2(
                        mousePos.x - (clickedNode.Position.x + canvasOffset.x),
                        mousePos.y - (clickedNode.Position.y + canvasOffset.y)
                    );
                    evt.Use();
                }
                else
                {
                    // 点击空白，取消选中，准备拖拽画布
                    selectedNode = null;
                    isDraggingCanvas = true;
                    dragStartMousePos = mousePos;
                    dragStartCanvasOffset = canvasOffset;
                    evt.Use();
                }
            }
            // 中键按下，拖拽画布
            else if (evt.button == 2)
            {
                isDraggingCanvas = true;
                dragStartMousePos = mousePos;
                dragStartCanvasOffset = canvasOffset;
                evt.Use();
            }
        }

        private void HandleMouseDrag(Event evt, Vector2 mousePos, bool isInCanvas)
        {
            // 拖拽节点
            if (isDraggingNode && selectedNode != null)
            {
                selectedNode.Position = new Vector2(
                    mousePos.x - dragNodeStartOffset.x - canvasOffset.x,
                    mousePos.y - dragNodeStartOffset.y - canvasOffset.y
                );
                evt.Use();
            }

            // 拖拽画布
            if (isDraggingCanvas)
            {
                canvasOffset = dragStartCanvasOffset + (mousePos - dragStartMousePos);
                evt.Use();
            }
        }

        private void HandleMouseUp(Event evt, Vector2 mousePos, bool isInCanvas)
        {
            // 结束拖拽状态
            if (isDraggingNode)
            {
                isDraggingNode = false;
                evt.Use();
            }

            if (isDraggingCanvas)
            {
                isDraggingCanvas = false;
                evt.Use();
            }
        }

        private void HandleRightClick(Event evt, Vector2 mousePos, bool isInCanvas)
        {
            if (!isInCanvas) return;

            // 检查右键是否点击到节点
            NodeData clickedNode = GetNodeAtPosition(mousePos);
            if (clickedNode != null)
            {
                selectedNode = clickedNode;
                ShowNodeRightClickMenu(clickedNode);
            }
            else
            {
                // 空白处右键菜单
                ShowCanvasRightClickMenu(mousePos);
            }
        }

        /// <summary>
        /// 处理连线状态下的鼠标点击
        /// </summary>
        private void HandleConnectingClick(Vector2 mousePos)
        {
            // 点击到有效节点，完成连线
            NodeData clickedNode = GetNodeAtPosition(mousePos);
            if (clickedNode != null && clickedNode != connectingFromNode)
            {
                // 防止重复连线
                if (!connectingFromNode.NextNodeGuids.Contains(clickedNode.Guid))
                {
                    connectingFromNode.NextNodeGuids.Add(clickedNode.Guid);
                }
            }

            // 无论点击什么，都结束连线状态
            isConnecting = false;
            connectingFromNode = null;
        }

        /// <summary>
        /// 处理键盘事件
        /// </summary>
        private void HandleKeyDown(Event evt)
        {
            // Delete键删除选中节点
            if (evt.keyCode == KeyCode.Delete && selectedNode != null && !selectedNode.IsRoot)
            {
                DeleteNode(selectedNode);
                evt.Use();
            }
        }

        /// <summary>
        /// 获取鼠标位置下的节点
        /// </summary>
        private NodeData GetNodeAtPosition(Vector2 mousePos)
        {
            foreach (var node in configData.Nodes)
            {
                Rect nodeRect = GetNodeRect(node);
                if (nodeRect.Contains(mousePos))
                {
                    return node;
                }
            }
            return null;
        }
        #endregion

        #region 右键菜单
        /// <summary>
        /// 画布空白处右键菜单（添加节点）
        /// </summary>
        private void ShowCanvasRightClickMenu(Vector2 mousePos)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("添加节点"), false, () =>
            {
                CreateNewNode(mousePos - canvasOffset);
            });
            menu.ShowAsContext();
        }

        /// <summary>
        /// 节点右键菜单
        /// </summary>
        private void ShowNodeRightClickMenu(NodeData node)
        {
            GenericMenu menu = new GenericMenu();

            // 添加连线
            menu.AddItem(new GUIContent("添加next"), false, () =>
            {
                StartConnecting(node);
            });

            menu.AddSeparator("");

            // 断开连线菜单
            if (node.NextNodeGuids.Count > 0)
            {
                menu.AddItem(new GUIContent("断开next/全部断开"), false, () =>
                {
                    node.NextNodeGuids.Clear();
                    Repaint();
                });
                menu.AddDisabledItem(new GUIContent("断开next/----------------"));
                // 列出所有已连接的节点
                foreach (var nextGuid in node.NextNodeGuids)
                {
                    var nextNode = configData.Nodes.Find(n => n.Guid == nextGuid);
                    if (nextNode != null)
                    {
                        menu.AddItem(new GUIContent($"断开next/断开 {nextNode.NodeName}"), false, () =>
                        {
                            DisconnectNext(node, nextGuid);
                        });
                    }
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("断开next"));
            }

            menu.AddSeparator("");

            // 删除节点（根节点禁用）
            if (node.IsRoot)
            {
                menu.AddDisabledItem(new GUIContent("删除节点"));
            }
            else
            {
                menu.AddItem(new GUIContent("删除节点"), false, () =>
                {
                    DeleteNode(node);
                });
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 添加属性的下拉菜单
        /// </summary>
        private void ShowAddPropertyMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (NodePropertyType propType in Enum.GetValues(typeof(NodePropertyType)))
            {
                menu.AddItem(new GUIContent(propType.ToString()), false, () =>
                {
                    AddPropertyToSelectedNode(propType);
                });
            }
            menu.ShowAsContext();
        }
        #endregion

        #region 核心操作方法
        /// <summary>
        /// 创建新节点
        /// </summary>
        private void CreateNewNode(Vector2 position)
        {
            var newNode = new NodeData
            {
                NodeName = "新节点",
                Position = position
            };
            configData.Nodes.Add(newNode);
            selectedNode = newNode;
            Repaint();
        }

        /// <summary>
        /// 开始连线流程
        /// </summary>
        private void StartConnecting(NodeData fromNode)
        {
            isConnecting = true;
            connectingFromNode = fromNode;
            connectingMousePos = Event.current.mousePosition;
            Repaint();
        }

        /// <summary>
        /// 断开指定连线
        /// </summary>
        private void DisconnectNext(NodeData fromNode, string nextGuid)
        {
            fromNode.NextNodeGuids.Remove(nextGuid);
            Repaint();
        }

        /// <summary>
        /// 删除节点，同时清理所有指向该节点的连线
        /// </summary>
        private void DeleteNode(NodeData nodeToDelete)
        {
            if (nodeToDelete.IsRoot) return;

            // 清理所有指向该节点的连线
            foreach (var node in configData.Nodes)
            {
                node.NextNodeGuids.Remove(nodeToDelete.Guid);
            }

            // 删除节点本身
            configData.Nodes.Remove(nodeToDelete);

            // 清空选中状态
            if (selectedNode == nodeToDelete)
            {
                selectedNode = null;
            }

            Repaint();
        }

        /// <summary>
        /// 给选中节点添加属性
        /// </summary>
        private void AddPropertyToSelectedNode(NodePropertyType propType)
        {
            if (selectedNode == null) return;
            selectedNode.Properties.Add(new NodeProperty(propType));
            Repaint();
        }

        /// <summary>
        /// 保存配置流程
        /// </summary>
        private void SaveConfig()
        {
            string fileName = EditorUtility.SaveFilePanel(
                "保存配置文件",
                XMLConfigSerializer.DefaultSavePath,
                "节点配置",
                "xml"
            );

            if (string.IsNullOrEmpty(fileName)) return;

            string pureFileName = Path.GetFileNameWithoutExtension(fileName);
            string savedPath = XMLConfigSerializer.SaveConfigToXML(configData, pureFileName);

            if (!string.IsNullOrEmpty(savedPath))
            {
                bool openFolder = EditorUtility.DisplayDialog("保存成功", $"配置已保存到：{savedPath}", "打开文件目录", "关闭");
                if (openFolder)
                {
                    EditorUtility.RevealInFinder(savedPath);
                }
            }
        }

        /// <summary>
        /// 清空面板，保留根节点
        /// </summary>
        private void ClearPanel()
        {
            var rootNode = configData.GetRootNode();
            configData.Nodes.Clear();
            // 重置根节点
            rootNode.Position = new Vector2(400, 300);
            rootNode.NextNodeGuids.Clear();
            rootNode.Properties.Clear();
            configData.Nodes.Add(rootNode);
            selectedNode = null;
            canvasOffset = Vector2.zero;
            Repaint();
        }
        #endregion
    }
}