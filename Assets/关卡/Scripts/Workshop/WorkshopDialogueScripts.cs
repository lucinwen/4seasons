internal static class WorkshopDialogueScripts
{
    internal static readonly WorkshopDialogueLine[] Prologue =
    {
        new WorkshopDialogueLine("工坊掌事", $"欢迎加入{WorkshopTitle.GameName}，和大家一起学习桑蚕丝织。"),
        new WorkshopDialogueLine("工坊掌事", "春天采桑浴蚕、喂蚕，夏天下簇缫丝备料，秋天采材织造染色攀花，冬天裁衣缝制。"),
        new WorkshopDialogueLine("工坊掌事", "先去春季公告栏，向蚕房师傅领任务吧。"),
    };

    // Spring
    internal static readonly WorkshopDialogueLine[] SpringBriefing =
    {
        new WorkshopDialogueLine("工坊掌事", "春天到了，咱们工坊又要开始忙起来啦。"),
        new WorkshopDialogueLine("工坊掌事", "蚕儿要长得好，第一件事情就是准备好它们的食物和住处。"),
        new WorkshopDialogueLine("玩家", "掌事，我们春天要先做什么呢？"),
        new WorkshopDialogueLine("工坊掌事", "到了春章先和蚕房师傅打个招呼。他最清楚眼前这批小蚕需要什么。"),
        new WorkshopDialogueLine("工坊掌事", "去吧，蚕房掌事已经在等你了。"),
    };

    internal static readonly WorkshopDialogueLine[] SpringMulberryIntro =
    {
        new WorkshopDialogueLine("蚕房师傅", "清明桑叶初展，先拿采桑篮，左右移动接住八片鲜叶。"),
        new WorkshopDialogueLine("蚕房师傅", "采好桑叶后捡蚕种纸，到浴蚕桶清洗。桑叶与干净蚕种备齐，就去养蚕区喂蚕。"),
    };

    internal static readonly WorkshopDialogueLine[] SpringBathIntro =
    {
        new WorkshopDialogueLine("蚕房师傅", "谷雨蚕事渐忙，把蚕种纸带到浴蚕平台。"),
        new WorkshopDialogueLine("蚕房师傅", "拿起擦拭布，蘸清水后轻轻擦去蚕种上的污渍，收好干净蚕种。"),
    };

    internal static readonly WorkshopDialogueLine[] SpringEggPaperCollected =
    {
        new WorkshopDialogueLine("玩家", "师傅，我找到蚕种纸了！"),
        new WorkshopDialogueLine("蚕房掌事", "很好，把它带到浴蚕桶旁边。咱们准备开始了。"),
    };

    internal static readonly WorkshopDialogueLine[] SpringBathComplete =
    {
        new WorkshopDialogueLine("蚕房师傅", "干净蚕种准备好了。"),
        new WorkshopDialogueLine("蚕房师傅", "带着桑叶去养蚕架，把叶子拖入蚕匾，照料刚孵出的小蚕。"),
    };

    internal static readonly WorkshopDialogueLine[] SpringReview =
    {
        new WorkshopDialogueLine("工坊掌事", "采桑、浴蚕与喂蚕都完成了。"),
        new WorkshopDialogueLine("工坊掌事", "提交蚕种、桑叶和蚕宝宝，领取蚕茧，开启夏季。你的道具会一直保留。"),
    };

    // Summer
    internal static readonly WorkshopDialogueLine[] SummerBriefing =
    {
        new WorkshopDialogueLine("工坊掌事", "小织匠，夏天到了！还记得春天照顾的那些蚕吗？"),
        new WorkshopDialogueLine("玩家", "记得！它们已经长大了吗？"),
        new WorkshopDialogueLine("工坊掌事", "是呀，蚕儿结茧以后，就到了收茧的时候。"),
        new WorkshopDialogueLine("工坊掌事", "接下来你要去找缫丝师傅。他会教你怎么把蚕茧变成一缕缕生丝。"),
        new WorkshopDialogueLine("玩家", "蚕茧里面真的能抽出丝来吗？"),
        new WorkshopDialogueLine("工坊掌事", "当然！不过可没有那么容易，去找师傅学学就知道了。"),
    };

    internal static readonly WorkshopDialogueLine[] SummerCocoonIntro =
    {
        new WorkshopDialogueLine("缫丝师傅", "立夏先下簇。把簇具上的同类蚕茧合成收好。"),
        new WorkshopDialogueLine("缫丝师傅", "完成原有合成消除小游戏，领取索绪。"),
    };

    internal static readonly WorkshopDialogueLine[] SummerCocoonComplete =
    {
        new WorkshopDialogueLine("玩家", "收好了！这些就是成熟的蚕茧吗？"),
        new WorkshopDialogueLine("缫丝师傅", "不错！收茧以后还要分拣和储藏。"),
        new WorkshopDialogueLine("缫丝师傅", "蚕茧可怕潮了，尤其是梅雨季节，湿热的环境容易影响茧丝质量。所以要先把它们好好保存起来！等到合适的时候，咱们再开始煮茧缫丝。"),
    };

    internal static readonly WorkshopDialogueLine[] SummerBoilingIntro =
    {
        new WorkshopDialogueLine("缫丝师傅", "蚕茧已经收好了，接下来就要准备煮茧。"),
        new WorkshopDialogueLine("玩家", "师傅，为什么要先煮茧呢？"),
        new WorkshopDialogueLine("缫丝师傅", "因为蚕茧表面的丝层紧紧缠在一起，煮茧能帮助软化茧层，让后面抽丝更顺利。"),
        new WorkshopDialogueLine("缫丝师傅", "咱们先准备好木柴和清水，再把蚕茧放进锅里。"),
        new WorkshopDialogueLine("玩家", "那我来找材料！"),
    };

    internal static readonly WorkshopDialogueLine[] SummerBoilingComplete =
    {
        new WorkshopDialogueLine("缫丝师傅", "木材和清水备齐，可以开始小满缫丝。"),
        new WorkshopDialogueLine("缫丝师傅", "先走通迷宫找到线轴，再回锅旁移动导丝环均匀绕丝。右侧缫丝车点「绕丝上轴」，就能进入丝路接接乐。"),
    };

    internal static readonly WorkshopDialogueLine[] SummerReview =
    {
        new WorkshopDialogueLine("工坊掌事", "下簇、缫丝绕轴和接水管备料已完成。"),
        new WorkshopDialogueLine("工坊掌事", "提交盐、索绪与线轴，获得丝束，进入秋季。"),
    };

    // Autumn
    internal static readonly WorkshopDialogueLine[] AutumnBriefing =
    {
        new WorkshopDialogueLine("工坊掌事", "秋章先到染材园认识和采集天然染材。"),
        new WorkshopDialogueLine("工坊掌事", "白露修机织造，秋分研磨染色与秋社，寒露攀花。梭子、缬和染布都不会消耗。"),
    };

    internal static readonly WorkshopDialogueLine[] AutumnRitualIntro =
    {
        new WorkshopDialogueLine("织造师傅", "白露、秋分前后，咱们到灵桑阙参加秋社，春祈秋报。"),
        new WorkshopDialogueLine("织造师傅", "第一次交谈时我会送你糕点。再收集香蕉与蚕花酒，登上灵桑阙提交贡品，领取土地公雕像。"),
    };

    internal static readonly WorkshopDialogueLine[] AutumnWeavingIntro =
    {
        new WorkshopDialogueLine("织造师傅", "白露先把织机修好。拾起织机左侧地上的木槌，在黄色区域内击打三次。"),
        new WorkshopDialogueLine("织造师傅", "再到工具箱拿梭子。用梭子织入、拔子拆回，按行列数字完成织造，就能收下缬。"),
    };

    internal static readonly WorkshopDialogueLine[] AutumnWarpComplete =
    {
        new WorkshopDialogueLine("织造师傅", "织造完成了。收下的缬可以带去青染轩染色。"),
        new WorkshopDialogueLine("织造师傅", "用采集的天然染材研磨染料，再挑喜欢的颜色染布。"),
    };

    internal static readonly WorkshopDialogueLine[] AutumnReview =
    {
        new WorkshopDialogueLine("工坊掌事", "采材、织造、染色、秋社和攀花完成了。"),
        new WorkshopDialogueLine("工坊掌事", $"提交土地公雕像、梭子和缬，领取{WorkshopProgress.ProductName(WorkshopSeason.Autumn)}。染布带入冬季裁衣，其他道具也一直保留。"),
    };

    // Winter
    internal static readonly WorkshopDialogueLine[] WinterBriefing =
    {
        new WorkshopDialogueLine("工坊掌事", "带上秋季的染布，到缝素堂晾干。先把前片、后片和袖片的衣样纸排在布上。"),
        new WorkshopDialogueLine("工坊掌事", "排好再沿虚线剪帛，剪完就能领取衣片。记得向缝衣师傅领取蚕连，再收好缝衣针。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterDyeMaterialsIntro =
    {
        new WorkshopDialogueLine("染整师傅", "立秋到染材园采集天然染材，入口旁有锄头。"),
        new WorkshopDialogueLine("染整师傅", "采到的材料有正常与损坏之分，按篮筐空间选择收下或丢弃。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterDyeMaterialsComplete =
    {
        new WorkshopDialogueLine("染整师傅", "染材收好了，到青染轩寻找研磨棒和研钵。"),
        new WorkshopDialogueLine("染整师傅", "把天然染材研磨成染料，秋分时给缬染上喜欢的颜色。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterGrindingIntro =
    {
        new WorkshopDialogueLine("染整师傅", "染材已经准备好了。接下来要把它们处理成适合使用的染料。"),
        new WorkshopDialogueLine("玩家", "要怎么做呢？"),
        new WorkshopDialogueLine("染整师傅", "把选好的材料放入研磨工具中，按照提示慢慢研磨。"),
        new WorkshopDialogueLine("染整师傅", "注意控制力度，尽量让材料变得细腻均匀。"),
        new WorkshopDialogueLine("玩家", "明白了！"),
    };

    internal static readonly WorkshopDialogueLine[] WinterGrindingComplete =
    {
        new WorkshopDialogueLine("玩家", "师傅，染料粉末已经准备好了！"),
        new WorkshopDialogueLine("染整师傅", "很好。接下来就可以给缬染色了。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterDyeingComplete =
    {
        new WorkshopDialogueLine("染整师傅", "染布做好了，缬和染料记录都保留。"),
        new WorkshopDialogueLine("染整师傅", "寒露回织造坊，用梭子和拔子在染布上攀花，再交付秋章。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterTailoringIntro =
    {
        new WorkshopDialogueLine("缝衣师傅", "小雪这天有冷雨。把染布晾在避雨又通风的架子上，干了再排衣样。"),
        new WorkshopDialogueLine("缝衣师傅", "把前片、后片和袖片拖到布上，避开布疵，衣样不能重叠。留一整块布做里襟，排好就可以下剪了。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterCuttingIntro =
    {
        new WorkshopDialogueLine("玩家", "师傅，这块布应该从哪里开始裁呀？"),
        new WorkshopDialogueLine("缝衣师傅", "衣样已经排好了。点一下剪刀，再沿着虚线拖动，剪出前衣、后衣和衣袖。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterCuttingComplete =
    {
        new WorkshopDialogueLine("缝衣师傅", "三块衣片都剪好了。"),
        new WorkshopDialogueLine("缝衣师傅", "收好衣片、缝衣针和蚕连，就可以回裁衣台选款缝衣。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterTailoringComplete =
    {
        new WorkshopDialogueLine("玩家", "师傅！我们的衣裳做好了！"),
        new WorkshopDialogueLine("染整师傅", "哈哈，真不错！从春天的一片桑叶，到今天这件完整的丝衣，你可参与了不少工序。"),
        new WorkshopDialogueLine("玩家", "原来一件丝衣要经过这么多人的劳动！"),
        new WorkshopDialogueLine("染整师傅", "是呀。桑树、蚕儿、蚕农、缫丝工、织造工和染整师傅，每个人都在其中付出了心血。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterReview =
    {
        new WorkshopDialogueLine("工坊掌事", "排布和剪帛都完成了。"),
        new WorkshopDialogueLine("工坊掌事", "提交缝衣针、蚕连和衣片，选心仪的款式，把衣片缝成成衣。"),
    };

    // Finale
    internal static readonly WorkshopDialogueLine[] FinaleBeforeDisplay =
    {
        new WorkshopDialogueLine("工坊掌事", "小织匠，来，把这件衣裳放到展台上吧。"),
        new WorkshopDialogueLine("玩家", "好！"),
    };

    internal static readonly WorkshopDialogueLine[] FinaleAfterDisplay =
    {
        new WorkshopDialogueLine("工坊掌事", "一片桑叶，看起来很普通。"),
        new WorkshopDialogueLine("工坊掌事", "可它养育了蚕儿；蚕儿吐出丝线；丝线织成绸布；绸布又经过染色、裁剪和缝制，最终成为一件衣裳。"),
        new WorkshopDialogueLine("玩家", "我终于知道，原来一件丝衣背后有这么多故事。"),
        new WorkshopDialogueLine("工坊掌事", "而这份手艺，已经在许多地方传承了很久很久。"),
        new WorkshopDialogueLine("玩家", "那以后还会有人继续做桑蚕丝织吗？"),
        new WorkshopDialogueLine("工坊掌事", "当然。只要有人愿意学习、愿意传承，这些技艺就不会轻易消失。"),
        new WorkshopDialogueLine("玩家", "我也想继续学习，把今天学到的知识告诉更多人！"),
        new WorkshopDialogueLine("工坊掌事", $"好！这才是{WorkshopTitle.GameName}的小织匠。"),
    };

    // Quest progress and return visits
    internal static readonly WorkshopDialogueLine[] SpringInProgress =
    {
        new WorkshopDialogueLine("蚕房师傅", "先拿采桑篮到桑园接叶，再拾蚕种纸到浴蚕桶清洗。"),
    };

    internal static readonly WorkshopDialogueLine[] SpringDone =
    {
        new WorkshopDialogueLine("蚕房师傅", "桑叶和干净蚕种备齐后，到蚕匾架拖叶喂蚕。获得蚕宝宝后回工坊交付春章成果。"),
    };

    internal static readonly WorkshopDialogueLine[] SummerInProgress =
    {
        new WorkshopDialogueLine("缫丝师傅", "立夏下簇取索绪，小满备水拾柴、寻线轴。锅旁练匀丝排线，右侧缫丝车点「绕丝上轴」玩丝路接接乐。"),
    };

    internal static readonly WorkshopDialogueLine[] SummerDone =
    {
        new WorkshopDialogueLine("缫丝师傅", "线轴仍要带回练丝坊缫丝，接通备料丝路后，再带盐、索绪和线轴交付。"),
    };

    internal static readonly WorkshopDialogueLine[] AutumnInProgress =
    {
        new WorkshopDialogueLine("织造师傅", "染材园采材，修机取梭织造获缬，青染轩研磨染色，再参加秋社、回织机攀花。"),
    };

    internal static readonly WorkshopDialogueLine[] AutumnDone =
    {
        new WorkshopDialogueLine("织造师傅", "染色后再回织机，用梭子和拔子按行列数字织纹样。完成秋社与攀花，再交付秋季任务。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterInProgress =
    {
        new WorkshopDialogueLine("缝衣师傅", "先把衣样纸排在布上，再沿虚线剪帛。备齐缝衣针、蚕连和衣片，就回裁衣台选款缝制。"),
    };

    internal static readonly WorkshopDialogueLine[] WinterDone =
    {
        new WorkshopDialogueLine("缝衣师傅", "成衣做好了。到锦衣堂试衣留念，恭喜晋升大织将！"),
    };
}
