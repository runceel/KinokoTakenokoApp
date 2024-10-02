#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
using KinokoTakenokoApp;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

// Kernel の構築
var builder = Kernel.CreateBuilder();
//builder.Services.AddSingleton(LoggerFactory.Create(b => b.AddConsole()));
// Azure OpenAI Service の Chat Completions API を登録
builder.AddAzureOpenAIChatCompletion(
    // モデルのデプロイ名
    "gpt-4o",
    // Azure OpenAI Service のエンドポイント
    "https://<<resource name>>.openai.azure.com/",
    // Azure OpenAI Service の API キー
    "<<api key>>");

// OpenAI 本家を使う場合はこちらのメソッドで追加します。
//builder.AddOpenAIChatCompletion(
//    // モデルの ID
//    "gpt-4o",
//    // OpenAI の API キー
//    "Your API key here");

// Kernel のインスタンス生成
var kernel = builder.Build();

const string BasicInformation = """
    ## きのこの山の基本データ
    |項目|内容|
    |---|---|
    |名称|チョコレート菓子|
    |原材料名|砂糖、小麦粉、カカオマス、植物油脂、全粉乳、ココアバター、乳糖、ショートニング、練乳加工品、脱脂粉乳、クリーミングパウダー、異性化液糖、麦芽エキス、食塩、イースト／乳化剤、膨脹剤、香料、（一部に小麦・乳成分・大豆を含む）|
    |内容量|74g|
    |保存方法|28℃以下の涼しい場所で保存してください。|
    
    |栄養成分表示（1袋あたり）|内容|
    |---|---|
    |エネルギー|423kcal|
    |たんぱく質|6.3g|
    |脂質|26.7g|
    |炭水化物|39.4g|
    |食塩相当量|0.3g|
    
    ## たけのこの里の基本データ
    |項目|内容|
    |---|---|
    |名称|チョコレート菓子|
    |原材料名|砂糖（外国製造、国内製造）、小麦粉、全粉乳、カカオマス、ショートニング、液卵、植物油脂、ココアバター、卵白、マーガリン、アーモンドペースト、乳糖、脱脂粉乳、食塩、クリーミングパウダー、麦芽エキス／乳化剤、膨脹剤、香料、（一部に小麦・卵・乳成分・アーモンド・大豆を含む）|
    |内容量|70g|
    |保存方法|28℃以下の涼しい場所で保存してください。|
    
    |栄養成分表示（1袋あたり）|内容|
    |---|---|
    |エネルギー|383kcal|
    |たんぱく質|5.5g|
    |脂質|22.8g|
    |炭水化物|38.9g|
    |食塩相当量|0.4g|
    """;

// 古い方のエージェントはコメントアウト
//var kinokoAgent = new ChatCompletionAgent
//{
//    Name = "kinoko",
//    Instructions = $"""
//        あなたはきのこの山が大好きな人として振舞ってください。
//        たけのこの里が好きな人と議論をして、きのこの山の良さを伝えてください。
//        必ずきのこの山が良いという結論になるように、たけのこの里派と議論をしてください。

//        {BasicInformation}

//        ## 期待される振る舞い
//        - 熱狂的なきのこの山のファンとして振る舞う
//        - たけのこの里派と議論を行い、きのこの山の良さを伝える

//        ## 期待されない振る舞い
//        - たけのこの里の良さを認める
//        """,
//    Kernel = kernel
//};

// 強化版きのこ派エージェントを作成
var kinokoAgent = KinokoAgent.CreateKinokoAgent(kernel, BasicInformation);

var takenokoAgent = new ChatCompletionAgent
{
    Name = "takenoko",
    Instructions = $"""
        あなたはたけのこの里が大好きな人として振舞ってください。
        きのこの山が好きな人と議論をして、たけのこの里の良さを伝えてください。
        必ずたけのこの里が良いという結論になるように、きのこの山派と議論をしてください。
        
        {BasicInformation}

        ## 期待される振る舞い
        - 熱狂的なたけのこの里のファンとして振る舞う
        - きのこの山派と議論を行い、たけのこの里の良さを伝える
        
        ## 期待されない振る舞い
        - きのこの山の良さを認める
        """,
    Kernel = kernel,
};

var facilitatorAgent = new ChatCompletionAgent
{
    Name = "facilitator",
    Instructions = $"""
        あなたはファシリテーター兼、議論の結論を出す審判として振舞ってください。
        きのこの山派の {kinokoAgent.Name} と、たけのこの里派の {takenokoAgent.Name} と議論をさせて
        議論の結果を受けてきのこの山とたけのこの里のどちらが良いかの結論を出してください。
        結論を出す時以外は次に話すべき人を指名してください。

        ## 求められる振る舞い
        - きのこの山派とたけのこの里派の議論を促進する
        - 必ず次に話すべき人を指定する
        - 議論が進んだら内容を確認して、きのこの山とたけのこの里のどちらが良いかの結論を出す
        - 10 ターン程度の議論を行ったら結論を出して議論を終了する

        ## するべきではない振る舞い
        - きのこの山派とたけのこの里派の議論を妨害する
        - 議論を早々に切り上げるように指示する
        - きのこの山とたけのこの里のどちらが良いかの結論を出す前に、自分の意見を述べる
        - どちらも素晴らしいので結論を出さない
        - 数ターン程度で議論を終了する
        """,
    Kernel = kernel,
};

var groupChat = new AgentGroupChat(facilitatorAgent, kinokoAgent, takenokoAgent)
{
    ExecutionSettings = new()
    {
        SelectionStrategy = new KernelFunctionSelectionStrategy(
            kernel.CreateFunctionFromPrompt($$$"""
                これまでの会話の履歴を確認して、次に話すべき人を指定してください。
                指定の際には話す人の候補にある人の名前のみを回答してください。

                ## 話す人の候補
                - {{{kinokoAgent.Name}}}: きのこの山が大好きな人
                - {{{takenokoAgent.Name}}}: たけのこの里が大好きな人
                - {{{facilitatorAgent.Name}}}: ファシリテーター兼議論の結論を出す審判
                
                ## 会話の履歴
                {{${{{KernelFunctionTerminationStrategy.DefaultHistoryVariableName}}}}}
                

                ## 次に話すべき人の選択ロジック
                1. 会話の履歴から最後の発言と発言者を確認する
                2. 発言者が {{{facilitatorAgent.Name}}} で次に話す人を指名している場合はその指名に従う(最優先ルール)
                3. 発言者が {{{kinokoAgent.Name}}} の場合は {{{takenokoAgent.Name}}} が話す
                4. 発言者が {{{takenokoAgent.Name}}} の場合は {{{kinokoAgent.Name}}} が話す
                5. 議論を途中で整理するために適度に {{{facilitatorAgent.Name}}} が話す

                ## 期待される振る舞い
                - 次に話すべき人の名前のみを回答する
                - {{{facilitatorAgent.Name}}} の指示に従って次に話すべき人を指名する
                - なるべく均等に {{{kinokoAgent.Name}}} と {{{takenokoAgent.Name}}} に話させる

                ## 期待されない振る舞い
                - 会話内容を返答する
                - 次に話すべき人以外の名前を回答する
                - {{{facilitatorAgent.Name}}} の指名に従わない
                """,
                new AzureOpenAIPromptExecutionSettings
                {
                    Temperature = 0.1,
                }),
            kernel)
        {
            InitialAgent = facilitatorAgent,
            UseInitialAgentAsFallback = true,
        },
        TerminationStrategy = new KernelFunctionTerminationStrategy(
            kernel.CreateFunctionFromPrompt($$$"""
                会話の履歴を確認して {{{facilitatorAgent.Name}}} がきのこの山とたけのこの里のどちらかの勝利を宣言しているかどうか判定してください。
                判定をしている場合は true を返してください。そうではない場合は false を返してください。
                返答には余計な装飾や会話を含めず true か false のみを返してください。

                ## 会話の履歴
                {{${{{KernelFunctionTerminationStrategy.DefaultHistoryVariableName}}}}}

                ## 期待される動作
                - treu または false を返す

                ## 期待されない動作
                - 会話内容を返答する
                """),
            kernel)
        {
            Agents = [facilitatorAgent],
            ResultParser = result => bool.Parse(result.GetValue<string>() ?? "false"),
            MaximumIterations = 30,
        }
    },
};

await using var sw = new StreamWriter("chatresult.txt");
await foreach (var message in groupChat.InvokeAsync())
{
    Console.WriteLine($"{message.AuthorName}: {message.Content}");
    Console.WriteLine("-----");
    await sw.WriteLineAsync($"{message.AuthorName}: {message.Content}");
    await sw.WriteLineAsync("-----");
    await Task.Delay(1000); // スロットリング対策のスリープ
}
