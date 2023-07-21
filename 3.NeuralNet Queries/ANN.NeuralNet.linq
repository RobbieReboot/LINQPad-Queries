<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Numerics.dll</Reference>
  <NuGetReference>Accord.Math</NuGetReference>
  <NuGetReference>StemmersNet</NuGetReference>
  <Namespace>Accord.Math</Namespace>
  <Namespace>Iveonik.Stemmers</Namespace>
  <Namespace>Accord.Statistics</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

void Main()
{
	var nc = new NeuralClassifier();
	
	List<string> vocabulary = new List<string>();
	List<Tuple<string,List<string>>> documents = new List<Tuple<string,List<string>>>();
	List<string> classes = new List<string>();
	//for each sentence, prep bag of words the size of the vocabulary
	foreach (var kvp in TrainingData.Data)
	{
		var cleanWords = nc.CleanWordList(kvp.Value,false,true);
		vocabulary = vocabulary.Union(cleanWords).ToList();
		documents.Add(new Tuple<string,List<string>>(kvp.Key,cleanWords));
		if (!classes.Contains(kvp.Key))
			classes.Add(kvp.Key);
	}

	var array_width = vocabulary.Count;
	double[,] training = new double[documents.Count,array_width];
	//item0 = class, item1 = stemmed/stopped wordlist.
	int tr = 0;
	var stride = array_width*sizeof(double);
	double[,] output = new double[documents.Count,classes.Count];
	
	foreach(var doc in documents) {
		var pattern_words = doc.Item2;	
		var bow = BagOfWords(pattern_words,vocabulary);
		Buffer.BlockCopy(bow,0,training,tr*stride,stride);
		var idx = classes.IndexOf(doc.Item1) ;
		if(idx== -1)
			Debugger.Break();
		if (tr>documents.Count)
			Debugger.Break();

		output[tr, classes.IndexOf(doc.Item1)] = 1;
		tr++;
	}
	nc.Classes = classes.ToArray();
	nc.Vocabulary = vocabulary;
	
	nc.TrainNetwork(training,output,20,0.1,20000,false,0.2);
	
	var result1 = nc.Think("Mineralogy of ochre deposits formed by the oxidation of iron sulfide",true);									//000
	var result2 = nc.Think("Texture and magnetic properties of nanocomposite Pr~2 Fe~1~4 B/alpha-Fe melt-spun ribbons",true);
	
	var cr1 = nc.Classify("Texture and magnetic properties of nanocomposite Pr~2 Fe~1~4 B/alpha-Fe melt-spun ribbons",true);				//530
	var cr2 = nc.Classify("Solution of the ground state wave function of Bose-condensed gas in a harmonic trap based on the Gross-Pitaevskii function", true);	//530

	var cr3 = nc.Classify("Shortening during Stimulation vs. during Relaxation: How Do the Costs Compare?");		//610

	var res1 = cr1.OrderByDescending(c => c.Value).Take(5).Dump("Texture and magnetic properties of nanocomposite Pr~2 Fe~1~4 B/alpha-Fe melt-spun ribbons");
	var res2 = cr2.OrderByDescending(c => c.Value).Take(5).Dump("Solution of the ground state wave function of Bose-condensed gas in a harmonic trap based on the Gross-Pitaevskii function");
	var res3 = cr3.OrderByDescending(c => c.Value).Take(5).Dump("Shortening during Stimulation vs. during Relaxation: How Do the Costs Compare?");
	var res4 = nc.Classify("On kinetic features of photo- or ?  -induced polymerization in p-diethynylbenzene crystals in the temperature range 4.2-300 K",true)
		.OrderByDescending(c => c.Value).Take(5).Dump("On kinetic features of photo- or ?  -induced polymerization in p-diethynylbenzene crystals in the temperature range 4.2-300 K");
	var res5 = nc.Classify("Protected Expression: Toward a Speaker-Oriented Theory",true)
	.OrderByDescending(c => c.Value).Take(5).Dump();
}

double[] BagOfWords(List<string> sentenceWords, List<string> vocabWords)
{
	//Fastest!
	var bag = new double[vocabWords.Count];
	foreach (var xx in sentenceWords)
	{
		var idx = vocabWords.IndexOf(xx);
		if (idx != -1)
			bag[idx] = 1;

	}
	return bag;

	//Other methods.
	// SLOWEST!
	//	var foundWords = vocabWords.AsEnumerable().Select((vocabWord, vocabIndex) => new { vocabWord, vocabIndex })
	//	.Join(sentenceWords.AsEnumerable().Select((sentenceWord, sentenceIndex) => new { sentenceWord, sentenceIndex }),
	//	v => v.vocabWord,
	//	s => s.sentenceWord,
	//	(vw, sw) => new { VocabWord = vw.vocabWord, VocabIndex = vw.vocabIndex, SentenceWord = sw.sentenceWord, SentenceIndex = sw.sentenceIndex })
	//	.ToList();
	//

	//FASTER
	//	var newlist = 
	//	from v in vocabWords
	//	join s in sentenceWords on v equals s
	//	select new { word = v };
	
	// Must be done for each of the abovemethods...
	//	foreach (var xx in foundWords)
	//	{
	//		bagOfWords[xx.VocabIndex] = 1;
	//	}
}

// Define other methods and classes here
public class NeuralClassifier
{
	public static List<string> StopWords;
	public static string StopWordsFileName = @"C:\Users\robhill\OneDrive\TBL\TBL 2017\TitleClassifier\CorpusGenerator\Stops320.txt"; //HOME!
	//public static string StopWordsFileName = @"C:\GitSouce\Sandbox\NeuralClassifier\NeuralClassifier\data\Stops320.txt"; //HOME!
	//public static string StopWordsFileName = @"C:\GitSource\neuralclassifier\NeuralClassifier\Data\Stops320.txt";	//WORK
	public bool Trained {get;set;}
	public string SynapseFile {get;set;}
	public List<string> Vocabulary { get; set; }
	public string[] Classes { get; set; }
	public List<string> Words;
	public Random rand;

	private double[,] synapse0;
	private double[,] synapse1;

	public NeuralClassifier(string synapseFile = @"c:\dumpzone\Synapses.json")
	{
		SynapseFile = synapseFile;

		//StopWords = File.ReadAllLines(StopWordsFileName).ToList();
		StopWords = MyExtensions.StopWordList;

		if (File.Exists(SynapseFile)) {
			LoadSynapses(SynapseFile);
			Trained=true;
		}
	}
	public void SaveSynapses(string filename)
	{
		JsonSerializer serializer = new JsonSerializer();
		//serializer.Converters.Add(new JavaScriptDateTimeConverter());
		serializer.NullValueHandling = NullValueHandling.Ignore;
		using (StreamWriter sw = new StreamWriter(filename))
		using (JsonWriter writer = new JsonTextWriter(sw))
		{
			var data = new Dictionary<string, double[,]>();
			data.Add("Synapse0", synapse0);
			data.Add("Synapse1", synapse1);
			serializer.Serialize(writer, data);
		}
		Trained = true;
	}
	public void LoadSynapses(string filename)
	{
		JsonSerializer serializer = new JsonSerializer();
		//serializer.Converters.Add(new JavaScriptDateTimeConverter());
		serializer.NullValueHandling = NullValueHandling.Ignore;
		using (StreamReader sw = new StreamReader(filename))
		using (JsonReader writer = new JsonTextReader(sw))
		{
			JObject data = (JObject)serializer.Deserialize(writer);
			var s0 = data["Synapse0"];
			synapse0 = JsonConvert.DeserializeObject<double[,]>(s0.ToString());
			var s1 = data["Synapse1"];
			synapse1 = JsonConvert.DeserializeObject<double[,]>(s1.ToString());
		}
	}

	public void TrainNetwork(double[,] trainingData, double[,] y, int hiddenNeurons = 10, double alpha = 1.0,
		int epochs = 100000, bool dropOut = false, double dropoutPercent = 0.5)
	{
		if (Trained)
			return;
		Console.WriteLine(
			$"Training with {hiddenNeurons} neurons, alpha:{alpha}, dropout:{dropOut} ({dropoutPercent})");
		Console.WriteLine($"Input matrix: {trainingData.GetLength(0)} x {trainingData.GetLength(1)}    Output matrix: 1x{Classes.Length}");
		rand = new Random(1);   //np.random.seed(1)

		double last_mean_error = 1.0;
		synapse0 = rand.Array(trainingData.GetLength(1), hiddenNeurons);
		synapse1 = rand.Array(hiddenNeurons, Classes.Length);

		var prev_synapse0_weight_update = new double[synapse0.GetLength(0), synapse0.GetLength(1)];
		var prev_synapse1_weight_update = new double[synapse1.GetLength(0), synapse1.GetLength(1)];

		var synapse0_direction_count = new double[synapse0.GetLength(0), synapse0.GetLength(1)];
		var synapse1_direction_count = new double[synapse1.GetLength(0), synapse1.GetLength(1)];

		for (var j=0;j<epochs+1;j++)
//		foreach (var j in Enumerable.Range(0, epochs + 1))
		{
			var layer_0 = trainingData;
//			var layer_1 = Sigmoid(layer_0.Dot(synapse0));
//			var layer_1 = Sigmoid(synapse0.Dot(layer_0));
//			var layer_1 = (synapse0.Dot(layer_0)).Apply(d => 1 / (1 + Math.Exp(-d)));
			var layer_1 = Matrix.Dot(layer_0,synapse0).Apply(d => 1.0 / (1.0 + Math.Exp(-d)));
			//if (dropOut)
			//{
			//    var bnd = new BinomialDistribution();
			/*layer_1 *= np.random.binomial([np.ones((len(X), hidden_neurons))], 1 - dropout_percent)[0] * (
								//                    1.0 / (1 - dropout_percent))*/
			//    layer_1 = layer_1.Select(b => );
			//}
			//var layer_2 = Sigmoid(layer_1.Dot(synapse1));
			var layer_2 = Matrix.Dot(layer_1,synapse1).Apply(d => 1.0 / (1.0 + Math.Exp(-d)));

			//var layer_2_error = y.Select((v, i) => v - layer_2[i]).ToArray();
			var layer_2_error = y.Subtract(layer_2);

//			var l2M = layer_2_error.Abs().Mean();
//			$"Delta after {j} iterations {l2M}.".Dump();
			if ((j % 1000) == 0)
			{
				var layer2Mean = layer_2_error.Abs().Mean();

				if (layer2Mean < last_mean_error)
				{
					Console.WriteLine($"Delta after {j} iterations {layer2Mean}.");
					last_mean_error = layer2Mean;
				}
				else
				{
					Console.WriteLine($"break: {layer2Mean} > {last_mean_error}");
					break;
				}
			}

			var layer_2_delta = layer_2_error.Multiply(SigmoidDerivative(layer_2));

			var layer_1_error = layer_2_delta.DotWithTransposed(synapse1);

			var layer_1_delta = layer_1_error.Multiply(SigmoidDerivative(layer_1));

			var synapse1_weight_update = layer_1.Transpose().Dot(layer_2_delta);
			var synapse0_weight_update = layer_0.Transpose().Dot(layer_1_delta);

			if (j > 0)
			{
				for (var r = 0; r < synapse0_direction_count.GetLength(0); r++)
				{
					for (var c = 0; c < synapse0_direction_count.GetLength(1); c++)
					{
						var curSyn0WeightUpdate = synapse0_weight_update[r, c] > 0 ? 1.0 : 0.0;
						var preSyn0WeightUpdate = prev_synapse0_weight_update[r, c] > 0 ? 1.0 : 0.0;
						synapse0_direction_count[r, c] += Math.Abs(curSyn0WeightUpdate - preSyn0WeightUpdate);
					}
				}
				for (var r = 0; r < synapse1_direction_count.GetLength(0); r++)
				{
					for (var c = 0; c < synapse1_direction_count.GetLength(1); c++)
					{
						var curSyn1WeightUpdate = synapse1_weight_update[r, c] > 0 ? 1.0 : 0.0;
						var preSyn1WeightUpdate = prev_synapse1_weight_update[r, c] > 0 ? 1.0 : 0.0;
						synapse1_direction_count[r, c] += Math.Abs(curSyn1WeightUpdate - preSyn1WeightUpdate);
					}
				}

			}

			synapse1 = synapse1.Add(synapse1_weight_update.Multiply(alpha));
			synapse0 = synapse0.Add(synapse0_weight_update.Multiply(alpha));

			prev_synapse0_weight_update = synapse0_weight_update.Copy();
			prev_synapse1_weight_update = synapse1_weight_update.Copy();

		}
		//Save the synapses.
//		var syn0Json = JsonConvert.SerializeObject(synapse0, Newtonsoft.Json.Formatting.Indented);
//		var syn1Json = JsonConvert.SerializeObject(synapse1, Newtonsoft.Json.Formatting.Indented);
		//SaveSynapses(SynapseFile);
	}

	//Compute sigmoid nonlinearity
	public double[,] Sigmoid(double[,] x)
	{
		return x.Apply(d => 1 / (1 + Math.Exp(-d)));
		
//		var output = new double[x.GetLength(0), x.GetLength(1)];
//
//		for (var r = 0; r < x.GetLength(0); r++)
//		{
//			for (var c = 0; c < x.GetLength(1); c++)
//			{
//				output[r, c] = 1 / (1 + Math.Exp(-x[r, c]));
//			}
//		}
//		return output;
	}

	//Convert output of sigmoid function to its derivative
	public double[,] SigmoidDerivative(double[,] x)
	{
		return x.Apply(d=>d*(1-d));
//		var output = new double[x.GetLength(0), x.GetLength(1)];
//
//		for (var r = 0; r < x.GetLength(0); r++)
//		{
//			for (var c = 0; c < x.GetLength(1); c++)
//			{
//				var cell = x[r, c];
//				output[r, c] = cell * (1 - cell);
//				//                    return x * (1 - x);
//			}
//		}
//		return output;
	}


	//return list of sentence words.
	public List<string> CleanWordList(string sentence, bool removeStopWords, bool useStemmer)
	{

		//Extract the words.
		var matchList = Regex.Matches(sentence, @"\w+(?:’|')(t|s|ll)|(\w+-\w+)|(\w{2,})");

		var words = matchList.Cast<Match>().Select(m => m.Value);

		//Remove the stopwords.
		var stopped = (removeStopWords ? words.Except(StopWords) : words).ToList();

		//Stem the words with Stemmers.Net
		var stemmer = new EnglishStemmer();
		var stemmed = useStemmer ? stopped.Select(m => stemmer.Stem(m)) : stopped;
		var cleanedUpSentence = stemmed.ToList();

		return cleanedUpSentence;
	}

	public double[] BagOfWords(string sentence, List<string> vocabulary)
	{
		var sentenceWords = CleanWordList(sentence,false,true);

		//Fastest!
		var bag = new double[vocabulary.Count];
		foreach (var xx in sentenceWords)
		{
			var idx = vocabulary.IndexOf(xx);
			if (idx != -1)
				bag[idx] = 1;
		}
		return bag;
	
	}

	public double[] Think(string sentence, bool showDetails = false)
	{
		var x = BagOfWords(sentence, Vocabulary);
		//Input Layer -os pir bag of words.
		var l0 = x;

		var l1 = l0.Dot(synapse0).Apply(d => 1 / (1 + Math.Exp(-d)));
		var l2 = l1.Dot(synapse1).Apply(d => 1 / (1 + Math.Exp(-d)));
		
	
		return l2;
	}
	public List<KeyValuePair<string,double>> Classify(string sentence,bool showDetails = false)
	{
		var results = Think(sentence,showDetails);
		var classResults = results.Select((r,i) => new KeyValuePair<string,double>(Classes[i],r)).ToList();
		return classResults;		
	}
}

public static class TrainingData
{
	public static List<KeyValuePair<string, string>> Data
	{
		get
		{
			return new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("530",
						"On the Vibration of Single-Walled Carbon Nanocones: Molecular Mechanics Approach versus Molecular Dynamics Simulations"),
					new KeyValuePair<string, string>("630",
						"The risk evaluation and control countermeasures of livestock waste pollution in Shanxi reservoir area based on GIS"),
					new KeyValuePair<string, string>("000",
						"Energetische Sanierung - Energiebedarf um über 50% gesenkt"),
					new KeyValuePair<string, string>("610",
						"Female rats selectively bred for high intrinsic aerobic fitness are protected from ovariectomy-associated metabolic dysfunction"),
					new KeyValuePair<string, string>("610",
						"Non-invasive Analysis of Proteins in Living Cells Using NMR Spectroscopy"),
					new KeyValuePair<string, string>("540",
						"Effects of Nonuniform Incident Illumination on the Thermal Performance of a Concentrating Triple Junction Solar Cell"),
					new KeyValuePair<string, string>("610",
						"Secondary non-Hodgkin lymphoma of the ethmoid sinus after temozolomide"),
					new KeyValuePair<string, string>("610", "Laboratory diagnostics of early rheumatoid arthritis"),
					new KeyValuePair<string, string>("000", "The Evolution of Bogolyubov&apos;s Renormalization Group"),
					new KeyValuePair<string, string>("570",
						"Stress Effect on Lipid Peroxidation and Physico-Chemical State of Membranes of Endoplasmic Reticulum of the Liver of Adult and Old Rats"),
					new KeyValuePair<string, string>("570",
						"Regulation of Phospholipid Turnover in Hamster Fibroblasts Transformed by Oncogenes v-src and N-ras"),
					new KeyValuePair<string, string>("530",
						"Lateral Distribution Function of EAS Electrons in Shower Size Range 10^5?N~e?3.10^7 According to Data of Maket-ANI Array"),
					new KeyValuePair<string, string>("000",
						"Spectral Synthesis of Diophantine and Nonlinear Controllers of Invertible Dynamical Systems"),
					new KeyValuePair<string, string>("550", "Neotectonics and Neogeodynamics of Central Europe"),
					new KeyValuePair<string, string>("610",
						"Involvement of Holinergic Mechanisms in the Operation of Specific Structures of a Visual Analyzer"),
					new KeyValuePair<string, string>("690", "Investigation of Vibrofilration of the Foundry Sewage"),
					new KeyValuePair<string, string>("530",
						"On kinetic features of photo- or ?  -induced polymerization in p-diethynylbenzene crystals in the temperature range 4.2-300 K"),
					new KeyValuePair<string, string>("570",
						"Effect of Polychlorinated Biphenyls on the Antioxidant System and Lipid Peroxidation in Gonads of the Black Sea Scorpionfish Scorpaena porcus L"),
					new KeyValuePair<string, string>("660", "Main principles of nutrition for learning youth"),
					new KeyValuePair<string, string>("660",
						"Up-to-Date State and Main Trends of Development of Wire Products Manufacturing"),
					new KeyValuePair<string, string>("000",
						"Elucidating the role of host-cell trafficking proteins in HIV-1 Nef downregulation of CD4-An RNAi based approach"),
					new KeyValuePair<string, string>("540",
						"^1H NMR for High-Throughput Screening to Identify Novel Enzyme Activity"),
					new KeyValuePair<string, string>("610",
						"Effects of Growth Factors and Estrogen on the Proliferation and Prolactin Gene Expression in Anterior Pituitary Cells of Rats"),
					new KeyValuePair<string, string>("630",
						"Advance in research and application of nonpoint-source agricultural pollution models"),
					new KeyValuePair<string, string>("610",
						"Circulatory drugs modify the hemodynamic actions of alfentanil combined with vecuronium or pancuronium"),
					new KeyValuePair<string, string>("000",
						"Subcellular Localization of Vegetative Storage Protein of Ginkgo biloba"),
					new KeyValuePair<string, string>("630",
						"Chemical and physical characteristics of pumice as a growing medium"),
					new KeyValuePair<string, string>("630",
						"First Results on the Performance of New Almond x Peach Hybrid Rootstocks Resistant to Nematodes on Almond Growth and Cropping"),
					new KeyValuePair<string, string>("630",
						"Physiological and Technological Barriers to Increasing Production Efficiency and Economic Sustainability of Peach Production Systems in California"),
					new KeyValuePair<string, string>("000",
						"The Behaviour of `Old Home&apos; x `Farmingdale&apos; Selections as Interstocks in Pear/Quince Combinations, in Rio Negro Valley, Argentina"),
					new KeyValuePair<string, string>("630",
						"Effects of Time of Pollination and of Pollen Source on Yield and Fruit Quality of the `Najda&apos; Date Palm Cultivar (Phoenix dactylifera L.) under Draa Valley Conditions in Morocco"),
					new KeyValuePair<string, string>("660",
						"Structure chemistry and bonding at grain boundaries in Ni~3Al - I. The role of boron in ductilizing grain boundaries"),
					new KeyValuePair<string, string>("610", "Biljezi parodontne destrukcije u slini pri parodontitisu"),
					new KeyValuePair<string, string>("570",
						"Taguchi optimisation of a multiplex pneumococcal serotyping PCR and description of 11 novel serotyping primers"),
					new KeyValuePair<string, string>("610",
						"General Paresis with Reversible Mesial Temporal T2-weighted Hyperintensity on Magnetic Resonance Image: A Case Report"),
					new KeyValuePair<string, string>("000",
						"Faunal associations of the Sarka Formation (Middle Ordovician, Darriwilian, Prague Basin, Czech Republic)"),
					new KeyValuePair<string, string>("630",
						"Remediation of benzo[a]pyrene-contaminated soil through its co-metabolism with soil microbes"),
					new KeyValuePair<string, string>("530",
						"Solution of the ground state wave function of Bose-condensed gas in a harmonic trap based on the Gross-Pitaevskii function"),
					new KeyValuePair<string, string>("620",
						"Algorithm of Hardy Function Interpolation and its Visualization in the Formation of the Virtual Battlefield Terrain"),
					new KeyValuePair<string, string>("620",
						"A Study on the Polishing Mechanism of Silicon Carbide (SiC) Optic Surface"),
					new KeyValuePair<string, string>("620",
						"Finite Element Simulation and Respond Surface Optimization on Stretch Bending of Square Tube Aluminum Profile"),
					new KeyValuePair<string, string>("620", "The Wool Shrink-Proof Technology of Sericin Bonding Wool"),
					new KeyValuePair<string, string>("620",
						"Research on Information Applied Technology with Swarm Intelligence for the TSP Problem"),
					new KeyValuePair<string, string>("650", "Bringing the Voices Together"),
					new KeyValuePair<string, string>("610",
						"The 20th anniversary of the 45 Central consulting and diagnostic polyclinic of Defense"),
					new KeyValuePair<string, string>("330",
						"Decontamination of the Liquid-Wastes Disposal Area by Byproduct Coke Plants"),
					new KeyValuePair<string, string>("530", "Uniform Germanium Nanoislets on Si(001)"),
					new KeyValuePair<string, string>("000", "The Paley-Wiener Theorem for Spaces of Sequences"),
					new KeyValuePair<string, string>("000",
						"Prospects of Technology of Processing Ores that Content Gold and Arsenic of the Ukrainian Board on the Concentrating Equipment in Essence Various Characteristics of Gravitational Fields"),
					new KeyValuePair<string, string>("610",
						"A new view of the assessment of a leukocytic reaction in coronary heart disease"),
					new KeyValuePair<string, string>("650",
						"Keynote Lecture 1: Ant Decision Systems for Combinatorial Optimization with Binary Constraints"),
					new KeyValuePair<string, string>("000",
						"D-Fructose-Mediated Stimulation of Bovine Lens Aldose Reductase Activation by UV-Irradiation"),
					new KeyValuePair<string, string>("620",
						"High-performance GaSb laser diodes and diode arrays in the 2.1-3.3 micron wavelength range for sensing and defense applications [9370-74]"),
					new KeyValuePair<string, string>("610",
						"Pilocarpine-Induced Epileptiform Activity of Isolated CA1 Hippocampal Neurons"),
					new KeyValuePair<string, string>("550",
						"Postsedimentational Transformations of Middle and Upper Riphean Deposits in the Baikal Region Sedimentary Basin"),
					new KeyValuePair<string, string>("530",
						"Features of the Interaction of High-Intensity Laser Radiation with a Dense Plasma"),
					new KeyValuePair<string, string>("620",
						"Nonlinear Differential Equations: Parametric Identification by Exact Polynomial Spline Schemes"),
					new KeyValuePair<string, string>("660",
						"Composite Wear-Resistant Glass Ceramic Materials with Various Fillets"),
					new KeyValuePair<string, string>("610",
						"Effective treatment of subtotal pancreonecrosis in a child aged 14 years"),
					new KeyValuePair<string, string>("610",
						"Shortening during Stimulation vs. during Relaxation: How Do the Costs Compare?"),
					new KeyValuePair<string, string>("610",
						"Clinical diagnostic significance of measuring serum activity of angiotensin converting enzyme"),
					new KeyValuePair<string, string>("620",
						"Study on Synthesizing and Characterizing of Zinc Ferrite Nanoparticles"),
					new KeyValuePair<string, string>("620",
						"A Study of the Installation and Adjustment Analysis Approach for Shaft Coupling Device with CAD"),
					new KeyValuePair<string, string>("620",
						"Genetic Algorithm and Support Vector Regression for Software Effort Estimation"),
					new KeyValuePair<string, string>("620",
						"HAAKE MiniLab: Erfassung des thermooxidativen Abbaus von Polyolefinen im Microcompounder"),
					new KeyValuePair<string, string>("000", "Optimizing Computer Technology Integration"),
					new KeyValuePair<string, string>("530",
						"Texture and magnetic properties of nanocomposite Pr~2 Fe~1~4 B/alpha-Fe melt-spun ribbons"),
					new KeyValuePair<string, string>("000",
						"Mineralogy of ochre deposits formed by the oxidation of iron sulfide"),
				};
		}
	}
}