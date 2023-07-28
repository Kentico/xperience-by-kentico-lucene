# KBankNews search example


## Register index

Add following code to Program.cs


```cs
	builder.Services.AddLucene(builder.Configuration, new[]
    {
         // ... other index definitions
         new LuceneIndex(
            typeof(KBankNewsSearchModel),
            new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
            KBankNewsSearchModel.IndexName
         ),
    });
```