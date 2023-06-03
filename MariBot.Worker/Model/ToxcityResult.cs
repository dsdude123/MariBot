namespace MariBot.Worker.Model
{

    public class ToxicityResult
    {
        public float[] toxicity { get; set; }
        public float[] severe_toxicity { get; set; }
        public float[] obscene { get; set; }
        public float[] identity_attack { get; set; }
        public float[] insult { get; set; }
        public float[] threat { get; set; }
        public float[] sexual_explicit { get; set; }
    }

}
