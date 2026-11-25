internal static class WorkshopObjectives
{
    public static void Describe(WorkshopItem item, out string title, out string shortHint)
    {
        var objective = For(item);
        title = objective.Title;
        shortHint = objective.ShortHint;
    }

    public static string Detail(WorkshopItem item) => For(item).Detail;

    public static string ChapterIntro(WorkshopSeason season)
    {
        string goal;
        switch (season)
        {
            case WorkshopSeason.Spring: goal = "先找蚕房师傅，采摘桑叶喂蚕，再备好蚕种。"; break;
            case WorkshopSeason.Summer: goal = "在场景里取盐、择茧和找线轴。"; break;
            case WorkshopSeason.Autumn: goal = "寻找织造工具，并为酬蚕备好贡品。"; break;
            case WorkshopSeason.Winter: goal = "先染布再裁衣，也记得找缝衣针。"; break;
            default: goal = "收集本季的三件关键道具。"; break;
        }

        return goal + "\nA / D 或左右方向键移动，空格跳跃，鼠标与道具或按钮互动。";
    }

    private static (string Title, string ShortHint, string Detail) For(WorkshopItem item)
    {
        switch (item)
        {
            case WorkshopItem.MulberryLeaf:
                return ("采摘桑叶", "先找蚕房师傅，再带采桑篮到桑树前。",
                    "到桑树前点击「采摘桑叶」或按 O，收下嫩叶。");
            case WorkshopItem.SilkwormEgg:
                return ("浴蚕", "喂蚕后捡蚕种纸，到浴蚕桶漂洗。",
                    "把蚕种筛放入水面，沿桶口轻晃漂洗，捞起杂质，给下一批备好蚕种。");
            case WorkshopItem.Silkworm:
                return ("喂蚕宝宝", "采桑后，带桑叶到蚕架前。",
                    "把四片桑叶逐片拖进蚕匾，喂饱小蚕后收下蚕宝宝。");
            case WorkshopItem.Salt:
                return ("取盐", "点击盐窖，获取盐。",
                    "点击场景中的盐窖，点「获取盐」，再点「收下」。");
            case WorkshopItem.Suoxu:
                return ("择茧", "到蚕茧簇架完成择茧，领取索绪。",
                    "走近蚕茧簇架，点「开始择茧」。点击亮起的图块，凑齐三个相同图案，完成后领取索绪。");
            case WorkshopItem.Spool:
                return ("找线轴", "取水、拾柴后，到煮茧锅进入迷宫。",
                    "点击水源取水，拾取木柴。走近煮茧锅点「寻找线轴」，在迷宫里找到线轴。");
            case WorkshopItem.Shuttle:
                return ("取梭", "点击织机附近的工具箱，领取梭子。",
                    "找到织机附近的工具箱，点击查看并获取梭子。织造时会用到它。");
            case WorkshopItem.RawSilkCloth:
                return ("织造", "找师傅拿锤子，修好织机，再用梭子织造。",
                    "找师傅领取锤子，走近织机点「修复织机」。修好并拿到梭子后，点「开始织造」。");
            case WorkshopItem.Statue:
                return ("酬蚕", "带酒、香蕉和糕点到供桌提交。",
                    "点击蚕花酒和香蕉果篮。向师傅领过锤子后，再对话领取糕点。把三件贡品交给供桌。");
            case WorkshopItem.DyedCloth:
                return ("染布", "找染材并研磨，再把素绸拖入染缸。",
                    "拾锄头进染料园找染材。回来拾研磨棒，点钵体研磨。到染缸选色，把素绸拖进去。");
            case WorkshopItem.CutPieces:
                return ("裁衣", "染好布后，在裁衣台剪出三片衣料。",
                    "染好布后，走近裁衣台点「开始裁衣」。点击剪刀，沿布上的线拖动，剪出三片衣料。");
            case WorkshopItem.Needle:
                return ("领针", "点击场景里的缝衣针并收下。",
                    "找到场景中的缝衣针，点击后选择「收下」。");
            default:
                return ("收集道具", "在关卡中寻找道具。", "在关卡中寻找道具并按提示操作。");
        }
    }
}
