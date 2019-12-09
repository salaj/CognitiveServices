using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class LuisIntent
{
    public string query { get; set; }
    public Prediction prediction { get; set; }
}

public class Prediction
{
    [JsonConstructor]
    public Prediction(string topIntent, JObject intents)
    {
        this.topIntent = topIntent;
        this.intents = intents.Children().Select(x =>
            new Intent(x.Path, x.First()["score"].ToObject<float>())).ToArray();
    }
    public string topIntent { get; set; }

    public float topScore => intents.First(x => x.name == topIntent).score;
    public Intent[] intents { get; set; }
}

public class Intent
{
    public Intent(string name, float score)
    {
        this.name = name;
        this.score = score;
    }
    public string name { get; set; }
    public float score { get; set; }
}

