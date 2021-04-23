/**
 * Created by Moon on 8/5/2019
 * This houses various structures to be used by both plugin and panel
 */

namespace TournamentAssistantShared
{
    public static class SharedConstructs
    {
        public const string Name = "比赛助手";
        public const string Version = "0.4.6";
        public const int VersionCode = 046;
        public static string Changelog =
            "0.0.1: 开始搭建协调员面板的UI\n" +
            "0.1.1: 实现版本控制系统\n" +
            "0.1.2: 修复歌曲下载问题\n" +
            "0.1.3: 重建歌曲详情，重构协调员进入比赛工作流，在插件处增加比赛选手退出按钮\n" +
            "0.1.4: 增加团队选项\n" +
            "0.1.5: 重新构建工作流，防止服务器断连\n" +
            "0.1.6: 更新二维码同步\n" +
            "0.1.7: 修复bug\n" +
            "0.1.8: 修复bug\n" +
            "0.1.9: 重添加无向模式\n" +
            "0.2.0: 防止玩家在游戏中暂停，增加mod列表显示\n" +
            "0.2.1: BeatKhana charity赛事版本\n" +
            "0.2.2: 修复bug，新增禁用失败\n" +
            "0.2.5: 增加禁用mod检查，修复bug\n" +
            "0.2.8: 修复服务器配置文件覆写的问题，增加IPV6支持，资格赛方面后端工作\n" +
            "0.3.0: 完成资格赛功能以及去中心化网络的实现\n" +
            "0.3.1: (由于Github的问题跳过该版本)\n" +
            "0.3.2: 增加从自定义服务器上下载资格赛设置\n" +
            "0.3.3: 更新以支持0.12.2\n" +
            "0.3.4: 修复二维码Fixed QR codes, 一起击剑, \"获取数据\"的bug\n" +
            "0.3.5: 修复了Oculus的bug\n" +
            "0.3.6: 切换到轮辐式网络，更新TAUI版本，修复叠加层精确度\n" +
            "0.3.7: 增加密码功能并在不死模式时禁用分数提交\n" +
            "0.3.8: 修复资格赛协调员工作流，有切实自定义排行榜\n" +
            "0.4.0: 更新以支持1.13.2, 合并websocket服务器, 修复资格赛排行榜, 重建网络以适应时间抓取\n" +
            "0.4.1: 修复了几个资格赛UI bug，合并玩家设置页\n" +
            "0.4.2: 更新版本号，修复直播流同步，重添加机器人通知(Alpha)\n" +
            "0.4.3: 更新以支持1.13.4\n" +
            "0.4.4: 修复修改项bug\n" +
            "0.4.5: 合并了Arimodu的更改: 增加直连，更新日志，励志名言，服务器自动更新器。机器人更新：新增资格赛排行榜分数更新推送消息给选手。插件更新：重新添加不死模式，客户端不再存储服务器列表。协调员/插件更新：重新开启 直播时禁用自定义方块 开关" +
            "0.4.6: 修复了资格赛不显示在列表中的问题，为不能使用自动暂停功能的玩家提供\"延迟开始\"选项, 修复了资格赛赛事创建/歌曲添加bug";

        public enum BeatmapDifficulty
        {
            Easy,
            Normal,
            Hard,
            Expert,
            ExpertPlus
        }
    }
}
