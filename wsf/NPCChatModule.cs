using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace wsf
{
    public class ApiResponse
    {
        public List<Choice>? choices { get; set; }
    }

    public class Choice
    {
        public Message? message { get; set; }
    }

    public class Message
    {
        public string? content { get; set; }
    }

    public class NPCChatModule
    {
        private static IModHelper? Helper;
        private static IMonitor? Monitor;
        private static readonly HttpClient httpClient = new HttpClient();

        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;
            
            // 注册对话事件
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        // 已移除不再需要的字段

        private static async void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // 检查是否点击了NPC
            if (e.Button.IsActionButton())
            {
                var tile = e.Cursor.GrabTile;
                foreach (NPC npc in Game1.currentLocation.characters)
                {
                    if (npc.Tile == tile && npc.CanSocialize)
                    {
                        // 优先使用原版对话
                        npc.checkForNewCurrentDialogue(Game1.player.getFriendshipHeartLevelForNPC(npc.Name), true);
                        Game1.drawDialogue(npc);
                        return;
                    }
                }
            }
            // 按U键开启主动对话
            else if (e.Button == SButton.U && Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new TitleTextInputMenu(
                    "和谢恩的远程聊天:", 
                    async (string msg) => {
                        if (!string.IsNullOrEmpty(msg))
                        {
                            string response = await GetAIResponse("Shane", msg);
                            NPC shane = Game1.getCharacterFromName("Shane");
                            if (shane != null)
                            {
                                shane.CurrentDialogue.Push(new Dialogue(shane, null, response));
                                Game1.drawDialogue(shane);
                            }
                            else
                            {
                                Game1.drawObjectDialogue(response);
                            }
                        }
                    }
                );
            }
            // ESC取消输入由TitleTextInputMenu自动处理
        }

        private static async Task<string> GetAIResponse(string npcName, string playerMessage)
        {
            try
            {
                // 配置API参数
                string apiUrl = "https://api.deepseek.com/v1/chat/completions";
                string apiKey = "sk-0027a11d4abb4a2b96f9204494f5c557"; 

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Headers.Add("Accept", "application/json");
                
                Monitor?.Log($"正在询问ljy制造的远程谢恩···", LogLevel.Debug);
                
                // 构造请求体
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new 
                        {
                            role = "system",
                            content = $"你正在扮演星露谷物语中的谢恩(Shane)，请用谢恩特有的忧郁、暴躁但善良的性格和语气回答问题。谢恩喜欢啤酒和养鸡，讨厌社交活动。要求：回复不要用markdown格式，如果有表示角色语气、动作的文字，用括号包裹"
                        },
                        new
                        {
                            role = "user",
                            content = playerMessage
                        }
                    }
                };

                // 设置请求内容及Content-Type
                var jsonContent = JsonContent.Create(requestBody);
                request.Content = jsonContent;
                
                // 发送请求并获取响应
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                // 解析响应
                var responseData = await response.Content.ReadFromJsonAsync<ApiResponse>();
                return responseData?.choices?.FirstOrDefault()?.message?.content ?? "抱歉，我现在不知道该说什么...";
            }
            catch (Exception ex)
            {
                Monitor?.Log($"调用Deepseek API失败: {ex.Message}", LogLevel.Error);
                return "抱歉，我现在无法回答你的问题...";
            }
        }
    }
}
