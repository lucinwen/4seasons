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
            case WorkshopSeason.Spring: goal = "清明拿篮采桑，谷雨浴蚕，再拖叶喂蚕。"; break;
            case WorkshopSeason.Summer: goal = "立夏下簇，小满寻线轴并缫丝，芒种夏至接水管备料。"; break;
            case WorkshopSeason.Autumn: goal = "立秋采染材，白露织造，秋分染色与秋社，寒露攀花。"; break;
            case WorkshopSeason.Winter: goal = "将秋季染布晾干排料，再剪帛、量体裁衣，找到针后选款缝衣。"; break;
            default: goal = "收集本季的三件关键道具。"; break;
        }

        return goal + "\nA / D 或左右方向键移动，空格跳跃，鼠标与道具或按钮互动。";
    }

    private static (string Title, string ShortHint, string Detail) For(WorkshopItem item)
    {
        switch (item)
        {
            case WorkshopItem.MulberryLeaf:
                return ("清明采桑", "先找蚕房师傅，再带采桑篮到桑树前。",
                    "到桑树前点「清明采桑」即可直接开始。用 A / D 或方向键左右移篮，接满 8 片干爽嫩叶。");
            case WorkshopItem.SilkwormEgg:
                return ("浴蚕", "采桑后捡蚕种纸，到浴蚕桶清洗。",
                    "拿起布蘸水，轻轻擦去蚕种表面的污渍，再收好干净蚕种。");
            case WorkshopItem.Silkworm:
                return ("谷雨喂蚕", "备好桑叶与干净蚕种，到养蚕区拖叶喂蚕。",
                    "在清明采桑、谷雨浴蚕后，前往蚕匾架，将三份桑叶拖给小蚕，完成后获得蚕宝宝。");
            case WorkshopItem.Salt:
                return ("取盐", "点击盐窖，获取盐。",
                    "点击场景中的盐窖，点「获取盐」，再点「收下」。");
            case WorkshopItem.Suoxu:
                return ("择茧", "到蚕茧簇架完成择茧，领取索绪。",
                    "走近蚕茧簇架，点「开始择茧」。点击亮起的图块，凑齐三个相同图案，完成后领取索绪。");
            case WorkshopItem.Spool:
                return ("找线轴", "取水、拾柴后，到煮茧锅进入迷宫。",
                    "点击水源取水，拾取木柴。走近煮茧锅点「寻找线轴」，在迷宫里找到线轴。锅旁可完成匀丝排线，右侧缫丝车点「绕丝上轴」进入丝路接接乐。");
            case WorkshopItem.Shuttle:
                return ("取梭", "点击织机附近的工具箱，领取梭子。",
                    "找到织机附近的工具箱，点击查看并获取梭子。织造时会用到它。");
            case WorkshopItem.RawSilkCloth:
                return ("织造", "拾起地上的锤子，修好织机，再用梭子织造。",
                    "点击拾取织机左侧地上的修机锤，走近织机点「修复织机」。修好并拿到梭子后，点「开始织造」。");
            case WorkshopItem.Statue:
                return ("酬蚕", "白露秋分带酒、香蕉、糕点到灵桑阙参加秋社。",
                    "第一次与织造师傅交谈即获得糕点，再点击蚕花酒和香蕉果篮。把三件贡品交给供桌。");
            case WorkshopItem.DyedCloth:
                return ("染布", "秋季采染材并研磨，再到染缸选择颜色。",
                    "拾锄头进染料园找染材，回来研磨。选好染料，把一块素绸拖进染缸，染好后带回织机攀花。");
            case WorkshopItem.CutPieces:
                return ("裁衣", "在裁衣台晾布排料，再剪出三片衣料。",
                    "带秋季染布到裁衣台。晾干后摆齐前片、后片和袖片，留出整块余布。完成排料后点击剪刀，沿虚线拖动，剪出三片衣料。");
            case WorkshopItem.Needle:
                return ("领针", "点击场景里的缝衣针并收下。",
                    "找到场景中的缝衣针，点击后选择「收下」。");
            default:
                return ("收集道具", "在关卡中寻找道具。", "在关卡中寻找道具并按提示操作。");
        }
    }
}
