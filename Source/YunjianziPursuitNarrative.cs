using System.Linq;

namespace Yunjianzi
{
    public sealed partial class GameComponent_YunjianziPursuit
    {
        private bool narrativeRefreshed;
        private bool spaceUnavailableNotified;

        internal static string QuestBody(int index)
        {
            return Text(index < 2 ? "SurfaceTask" : "SpaceTask") + "\n\n" + Text("TaskInstructions");
        }

        public string ProgressText(int index)
        {
            // Derive progress from saved completion states, never from damage or letters.
            var completed = nodes.Where(n => n.complete && n.index >= 0 && n.index < Names.Length)
                .Select(n => n.index).Distinct().ToList();
            int count = completed.Count(i => (i < 2) == (index < 2));
            return $"【本阶段已根除节点：{count}/{(index < 2 ? 2 : 3)}】\n【已根除节点：{completed.Count}/{Names.Length}】";
        }

        internal void RefreshNarrative()
        {
            if (narrativeRefreshed) return;
            narrativeRefreshed = true;
            // Existing quests store their original prose in the save; refresh presentation only.
            foreach (var node in nodes.Where(n => n.quest != null && n.index >= 0 && n.index < Names.Length))
            {
                node.quest.name = QuestTitle(node.index);
                node.quest.description = QuestBody(node.index);
            }
        }
    }
}
