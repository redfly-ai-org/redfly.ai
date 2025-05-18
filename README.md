# redfly.ai

_Never lose a customer to the database again._

Our team has been building data-driven apps for 20+ years. Tired of fighting database performance, scalability, and cost, we realized that it's possible to bypass most of the issues with disk-based databases if we could effectively cache all our reads, using the database mainly as a data store with Redis as the front end. 

This enables interacting with data using strongly typed, object-oriented paradigms, enabling a grab-and-go coding style more intuitive to application developers than SQL queries. This has never been possible before. We did not believe it would work for an application before extensive R&D and solving numerous problems along the way. 

Some of the largest companies in the world synchronize their database directly with the cache so that they can seamlessly access their data without worrying about optimizing SQL queries or scaling up their disk-based databases. Now, for the first time in history, we have made it possible for anybody to implement the same thing without spending several million dollars and years in R&D costs. 

redfly.ai is the world's first schema-agnostic caching system. No other company has done or will do this. Why build a data-agnostic system when you could _more_ easily (still non-trivial) make something that works solely with your database? This made it possible to cache any database. The second step was to build on that foundation a new way of interacting with the database, which we could only imagine for many years.

redfly.ai lets you synchronize your database with Redis <i>transparently</i> and <i>generate</i> a data access layer that <i>integrates</i> data access code with caching. This open-source repo is intended to make it easy for developers to understand and try out a novel system that is sure to fire all their neurons when they see what is truly possible.

**Goals**

Provide source code that:

1. **RedflyPerformanceTest** Project: Lets you easily verify that our system performs better than conventional techniques for data access at scale (Done ✔️).

2. **RedflyDatabaseSyncProxy** Project: Provides a way for anybody to test our Redis synchronization service on demand
   - Get the database ready for synchronization by adding the functionality to prep it (Done ✔️)
   - Support for synchronizing databases which are hosted online (Done ✔️)
      - **SQL Server** Support (Available since day 1 ✔️)
      - **Postgres** Support (Done ✔️)
      - **MongoDB** Support (Done ✔️, Dog food Mongo Caching for our core Infrastructure TBD ⏳) 
      
3. **RedflyDataAccessClient** Project: Generates strongly typed client code based on your database schema that retrieves your data mostly from Redis, using the database only as a failback mechanism (Concept/ Design - WIP🏃🏽‍♀️‍➡️, Implementation TBD ⌚).

This code is not intended to be used as a best-practice implementation. It is focused on doing what it needs to do with minimal implementation time. 

We do all this setup work for our customers within our environment. This should let anyone get a taste of our technology without manual work on our part.

These applications do not require complex configuration or modifications to work. They will be simple console applications written in C# and using the latest version of .NET Core. 

**Pre-requisites**

Authentication is necessary to connect to our cloud services hosted on Azure. You can register with us here:<br/>
https://transparent.azurewebsites.net/Identity/Account/Register

Check your junk folder for emails from redfly.ai. Since this is a new domain, emails will go to the junk folder. No - there is no way to spoof this. Domain trust & reputation is earned over time (as it should be).

The proof is in the pudding. All our cloud services run on our technology.  

**Compatibility**

We currently support Postgres, MongoDB & SQL Server sync with Redis. We intend to support other relational databases (like Oracle, MySQL, etc) in the future. Eventually, we plan to support all **disk-based** databases and other public clouds like AWS and GCP. 

_We have a list of customers in our queue. If interested, please let us know at developer at redfly dot ai_.

**Caveats**

Relational Databases perform well for a small number of rows without a lot of use. The longer and harder you run the test (feel free to pound it), and the more data is in the DB table, the better redfly.ai will perform relative to SQL.

A better performance test can be found here: https://transparent.azurewebsites.net/fusioncore-demo. 

**Trust**

We are funded by the <a href="https://www.alchemistaccelerator.com/">Alchemist Accelerator</a> - the #1 accelerator for Enterprise Startups.

Under an NDA, customers can get more source code than what is available in our public repo. However, we do not expect the complete source code to be useful to the vast majority of developers who lack the specialized knowledge or experience in synchronization and other technologies underlying our core platform. 

**Documentation**

Higher-level overview: https://redfly.ai <br/>
More technical: https://nautilus2k.netlify.app <br/>

**Sales**

In the Azure Marketplace:<br/>
https://azuremarketplace.microsoft.com/en-us/marketplace/apps/redfly.redfly-offer-1?tab=overview

In the Azure Portal:<br/>
https://portal.azure.com/#view/Microsoft_Azure_Marketplace/GalleryItemDetailsBladeNopdl/id/redfly.redfly-offer-1/selectionMode~/false/resourceGroupId//resourceGroupLocation//dontDiscardJourney~/false/selectedMenuId/home/launchingContext~/%7B%22galleryItemId%22%3A%22redfly.redfly-offer-1dpssp%22%2C%22source%22%3A%5B%22GalleryFeaturedMenuItemPart%22%2C%22VirtualizedTileDetails%22%5D%2C%22menuItemId%22%3A%22home%22%2C%22subMenuItemId%22%3A%22Search%20results%22%2C%22telemetryId%22%3A%22d25c8cae-a836-4037-a2dd-9cd2f8e9e001%22%7D/searchTelemetryId/2783b73c-f204-4ae8-a1f7-b5bb83db4e73
