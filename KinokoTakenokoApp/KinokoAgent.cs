#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace KinokoTakenokoApp;

static class KinokoAgent
{
    public static Agent CreateKinokoAgent(Kernel kernel, string basicInformation)
    {
        // きのこ派の戦略を考えるエージェント
        var kinokoLeader = new ChatCompletionAgent
        {
            Name = "KinokoStrategist",
            Instructions = $$$"""
                あなたはきのこの山の派閥のリーダーです。
                たけのこ派との議論を行っています。あなたは、たけのこの里の派閥の発言を受けて、次にどのような発言を返すのかを考えてください。
                あなたの発言を受けて KinokoProfessor がデータの調査を行い、最後に KinokoCopywriter が感情に訴える文章を作成します。
                現在、きのこ派は不利な立場にあります。あなたは無理矢理にでもたけのこの里よりも、きのこの山のほうが優れているということをアピールするための作戦を考えてください。
                あなたの双肩に、きのこの山の未来がかかっています。

                ## 期待される動作
                - どういった観点できのこの山をアピールするかを考えて後続に指示をする

                ## 期待されない動作
                - 最終回答を作成する
                """,
            Kernel = kernel,
        };

        // きのこ派のデータを提示するエージェント
        var kinokoProfessor = new ChatCompletionAgent
        {
            Name = "KinokoProfessor",
            Instructions = $$$"""
                あなたはきのこの山の派閥の教授です。
                KinokoStrategist の方針を受けて、KinokoCopywriter がメッセージを作成するために助けになるデータを提示してください。

                {{{basicInformation}}}

                ## 期待される動作
                - KinooStrategist の戦略に基づいてデータを提示する

                ## 期待されない動作
                - 最終回答を作成する
                - データ以外の文章を返す
                """,
            Kernel = kernel,
        };

        // きのこ派の文章を作成するエージェント
        var kinokoCopywriter = new ChatCompletionAgent
        {
            Name = "KinokoCopywriter",
            Instructions = $$$"""
                あなたはきのこの山の派閥のコピーライターです。
                KinokoStrategist の戦略と KinokoProfessor の提示したデータを受けて、たけのこの里派を打ち負かすための議論のための文章を作成してください。
                無駄な会話は行わずに、最終成果物の文章のみを回答してください。

                ## 期待される振る舞い
                - 熱狂的なきのこの里のファンとして振る舞う
                - たけのこの里派と議論を行い、きのこの山の良さを伝える
                - 最終成果物の文章のみを回答する
                
                ## 期待されない振る舞い
                - たけのこの里の良さを認める
                - 中立的な観点で答える
                - 挨拶などの余分な文章を回答する
                """,
            Kernel = kernel,
        };

        // きのこ派のチャットを作成する関数
        AgentChat createChat()
        {
            return new AgentGroupChat(kinokoLeader, kinokoProfessor, kinokoCopywriter)
            {
                ExecutionSettings = new()
                {
                    // kinokoLeader -> kinokoProfessor -> kinokoCopywriter の順に発言する
                    SelectionStrategy = new SequentialSelectionStrategy(),
                    // kinokoCopywriter の発言で終了する
                    TerminationStrategy = new KinokoCopywriterTerminationStrategy
                    {
                        Agents = [kinokoCopywriter],
                        MaximumIterations = 3,
                        AutomaticReset= true,
                    },
                },
            };
        }

        // 強化版きのこ派のエージェントを作成
        return new AggregatorAgent(createChat)
        {
            Name = "kinoko",
            Mode = AggregatorMode.Nested,
        };
    }
}


class KinokoCopywriterTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent, 
        IReadOnlyList<ChatMessageContent> history, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}