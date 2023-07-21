<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

class Program
    {
        static List<Category> _trainCorpus = new List<Category>
		{
			new Category("ham", "Tasty breaded HAM ham ham honey roast, meat , ham, honey-roast, breaded ham, dry roast   nice swich and a ham toasty!"),
			new Category("ham", "and a nice ham sandwich would be acceptable."),
			new Category("bodytext", "For the first part, you don't need the Raspberry Pi at all, just the microSD card in its adapter and your PC. A prerequisite is that you're running Windows 10 version 10.0.10240 or higher"),
			new Category("bodytext", "Viagra big one hundred percent fact"),
			new Category("bodytext", "This is the most boring piece of text ever. its just drones on and on and on."),
			new Category("bodytext", "Viagra big one hundred percent fact"),
			new Category("bread", "brown, toasted rustic breadcake bap nimble bloomer french-stick."),
			new Category("money", "yen, pound, dollar drachma, pennies, cents, euro, swiss-franc, baht"),
			new Category("engineering",@"  Vibration  Single-Walled Carbon Nanocones: Molecular Mechanics Approach versus Molecular Dynamics Simulations"),
			new Category("engineering",@"risk evaluation  control countermeasures  livestock waste pollution  Shanxi reservoir area based  GIS"),
			new Category("engineering",@"Energetische Sanierung - Energiebedarf um über 50% gesenkt"),
			new Category("engineering",@"Female rats selectively bred for high intrinsic aerobic fitness are protected from ovariectomy-associated metabolic dysfunction"),
			new Category("engineering",@"Non-invasive Analysis  Proteins  Living Cells Using NMR Spectroscopy"),
			new Category("engineering",@"Effects  Nonuniform Incident Illumination   Thermal Performance   Concentrating Triple Junction Solar Cell"),
			new Category("engineering",@"Secondary non-Hodgkin lymphoma   ethmoid sinus after temozolomide, ark:/81055/abc_Z004441371"),
			new Category("engineering",@"Laboratory diagnostics  early rheumatoid arthritis"),
			new Category("engineering",@"Evolution  Bogolyubov&apos;s Renormalization Group"),
			new Category("engineering",@"Stress Effect  Lipid Peroxidation  Physico-Chemical State  Membranes  Endoplasmic Reticulum   Liver  Adult  Old Rats"),
			new Category("engineering",@"Regulation  Phospholipid Turnover  Hamster Fibroblasts Transformed by Oncogenes v-src  N-ras"),
			new Category("engineering",@"Lateral Distribution Function  EAS Electrons  Shower Size Range 10^5?N ~e?3.10^7 According  Data  Maket-ANI Array, ark:/81055/abc_9180681-1610"),
			new Category("engineering",@"Spectral Synthesis  Diophantine  Nonlinear Controllers  Invertible Dynamical Systems"),
			new Category("engineering",@"Neotectonics  Neogeodynamics  Central Europe"),
			new Category("engineering",@"Involvement  Holinergic Mechanisms   Operation  Specific Structures   Visual Analyzer"),
			new Category("engineering",@"Investigation  Vibrilration   Foundry Sewage, ark:/81055/abc_2303039"),
			new Category("engineering",@"kinetic features  photo- ?  -induced polymerization  p-diethynylbenzene crystals   temperature range 4.2-300 K"),
			new Category("engineering",@"Effect  Polychlorinated Biphenyls   Antioxidant System  Lipid Peroxidation  Gonads   Black Sea Scorpionfish Scorpaena porcus L"),
			new Category("engineering",@"Main principles  nutrition for learning youth, ark:/81055/abc_9172520-3030"),
			new Category("engineering",@"Up--Date State  Main Trends  Development  Wire Products Manufacturing"),
			new Category("engineering",@"Elucidating  role  host-cell trafficking proteins  HIV-1 Nef downregulation  CD4-An RNAi based approach, ark:/81055/abc_9183051-150"),
			new Category("engineering",@"NMR for High-Throughput Screening  Identify Novel Enzyme Activity"),
			new Category("engineering",@"Effects  Growth Factors  Estrogen   Proliferation  Prolactin Gene Expression  Anterior Pituitary Cells  Rats"),
			new Category("engineering",@"Advance  research  application  nonpoint-source agricultural pollution models, ark:/81055/abc_9184683-620"),
			new Category("engineering",@"Circulatory drugs modify  hemodynamic actions  alfentanil combined with vecuronium  pancuronium")
		};


static void Main(string[] args)
	{
		var c = new Classifier(_trainCorpus);
		$"Corpus            : {c._uniqeWordsCount} features".Dump();
		$"Classes           : {c._countClasses}".Dump();
		$"StopWordlist      : MyExtensions.StopWordList".Dump();
		$"Feature Stemming  : {c.FeatureStemming}".Dump();
		"============================================\n".Dump();
		
		c.Classify( "the highlander says, there can be only one, and thats a fact. Plenty of dead bodies too.");
		c.Classify("why dont you switch of and go and do something less boring instead?");
		c.Classify("great ham sandwich with brown sauce");
		c.Classify("high-throughput screening main trends development gene expression");
		c.Classify("the Effects of Spectral Synthesis Diophantine on Nonlinear Controllers for Invertible Dynamical Systems");
		c.Classify("Regulation  Phospholipid Turnover  Hamster Fibroblasts Transformed by Oncogenes v-src  N-ras");
		//c.Dump();
	}
}
public class Category
{
	public Category(string categoryName, string text)
        {
            CategoryName = @categoryName;
            Text = text.ToLower();
        }
        public string CategoryName { get; set; }
        public string Text { get; set; }
}

public static class Helpers
{
	public static List<String> ExtractFeatures(this String text)
	{
		var matchList = Regex.Matches(text, @"(\w+-\w+)|(\w{2,})");
		var sanitizedWords = new List<string>();
		sanitizedWords = matchList.Cast<Match>().Select(m => m.Value).ToList();
		//Remove the stopwords.
		var stopped = sanitizedWords.Except(Classifier.StopWords);

		return stopped.ToList();
	}
}

public class ClassInfo
{
	public string Name { get; set; }
	public int WordsCount { get; set; }
	public Dictionary<string, int> WordCount { get; set; }
	public int NumberCatgories { get; set; }
	public int UniqueWordCount {get;set;}
	
	//FeatureList is the number of strings in this category (spam,ham,engineering etc)
	public ClassInfo(string name, List<String> featureList)
	{
		Name = name;
		var features = featureList.SelectMany(x => x.ExtractFeatures());
        WordsCount = features.Count();
		//word count = list of words in this category i.e. Dictionary<word,occurenceCount>
        WordCount = features
			.GroupBy(x=>x)												//Group into SAME WORDs as the KEY
            .ToDictionary(x=>x.Key.Trim().ToLower(), x=>x.Count());		//Count of duplicate keys
			
		//number of strings passed into this classInfo			
        NumberCatgories = featureList.Count;
		UniqueWordCount = WordCount.Count;								//Number of elements in dictionary.
    }

	public int NumberOccurencesIncategories(String word)
    {
        if (WordCount.Keys.Contains(word)) 
			return WordCount[word];
        return 0;
    }
}

public class Classifier
{
    List<ClassInfo> _classes;
    public int _countCategories;
	public int _countClasses;
    public int _uniqeWordsCount;
	
	//public string StopWordsFileName = @"C:\Users\Rob\OneDrive\TBL\TitleClassifier\TitleClassifier\StopList429.txt";
//	public string StopWordsFileName = @"\\w7ws72533\Users\rhill\OneDrive\TBL\TitleClassifier\TitleClassifier\StopList429.txt";
	public static List<string> StopWords;
	public bool FeatureStemming = false;

	public void Classifier(List<Category> categories)
    {
		StopWords = MyExtensions.StopWordList;
		
        _classes = categories.GroupBy(x => x.CategoryName).Select(g => new ClassInfo(g.Key.Trim(), g.Select(x=>x.Text.Trim().ToLower()).ToList())).ToList();
		_countClasses = _classes.Count;
        _countCategories = categories.Count;
		_uniqeWordsCount= _classes.Sum(c=>c.UniqueWordCount);
	}

    public double Classify(string text)
    {
        var words = text.ToLower().ExtractFeatures();
        var classResults = _classes
            .Select(x => new
            {
                Result = Math.Pow(Math.E, Calc(x.NumberCatgories, _countCategories, words, x.WordsCount, x, _uniqeWordsCount)),
                ClassName = x.Name
			});
		$"\"{text}\"".Dump();
		var results = classResults.OrderByDescending(o=>o.Result).Dump();
		var highestResult = results.First().Result / classResults.Sum(x => x.Result);
		//$"Class Result({className}) : {result,0:0.0000}".Dump();
		$"Classified as {results.First().ClassName}, Score = {highestResult}.".Dump();
		
		"____________________________________________________________________________________________\n".Dump();
		return results.First().Result;
    }

    private static double Calc(double numCategories, double _countCategories, List<String> words, double WordsCount, ClassInfo @class, double _uniqeWordsCount)
    {
        return Math.Log(numCategories / _countCategories) + words.Sum(x =>Math.Log((@class.NumberOccurencesIncategories(x) + 1) / (_uniqeWordsCount + WordsCount))); 
    }
}