# redfly.ai
redfly.ai lets you synchronize your database with Redis <i>transparently</i> and <i>generate</i> a data access layer that <i>integrates</i> data access code with caching. This open-source repo is intended to make it easy for developers to understand and try out our system.

**Goals**

Provide source code that:

1. Let you easily verify that our system performs better than conventional techniques for data access at scale.
2. Provide a way for anybody to test our Redis synchronization service from anywhere on-demand (TBD).
3. Provide a way for anybody to generate their data access backend services on our cloud on-demand (TBD).

This code is not intended to be used as a best-practice implementation. It is focused on doing what it needs to do with minimal implementation time.

Even though these are intended as fully functional demos, items 2 and 3 are ambitious and complex to implement. We do all this setup work for our customers within our environment. This should let anyone get a taste of our technology without manual work on our part.

These applications do not require complex configuration or modifications to work. They will be simple console applications written in C# and using the latest version of .NET Core. 

**Pre-requisites**

Authentication is necessary to connect to our cloud services hosted on Azure. You can register with us here:<br/>
https://transparent.azurewebsites.net/Identity/Account/Register

Check your junk folder for emails from redfly.ai. Since this is a new domain, emails will go to the junk folder.

The proof is in our pudding. All our cloud services run on our technology.  

**Compatibility**

We currently support SQL Server, Redis, Azure Search, and Azure Cloud. We intend to support other relational databases (like Postgres) in the future. Eventually, we plan to support all disk-based databases and other public clouds like AWS and GCP. 

_We have a list of customers who are waiting for Postgres support. If interested, please let us know at developer at redfly dot ai_.

**Caveats**

Relational Databases perform well for a small number of rows without a lot of use. The longer and harder you run the test (feel free to pound it), and the more data is in the DB table, the better redfly.ai will perform relative to SQL.

A better performance test can be found here: https://transparent.azurewebsites.net/fusioncore-demo. 

**Solution**

We are very good at handling all the basic calls to a database, which adds up under load and prevents your customers from running more complex queries. Most secure applications make many database calls to render anything within an application. Think about it: why should everything in your app run slowly because a few users are running some expensive queries? We have taken this for granted too long. Why should security implementation slow down an app? That is what we are good at solving.

**Documentation**

Higher-level overview: https://redfly.ai <br/>
More technical: https://nautilus2k.netlify.app <br/>

**Sales**

In the Azure Marketplace:<br/>
https://azuremarketplace.microsoft.com/en-us/marketplace/apps/redfly.redfly-offer-1?tab=overview

In the Azure Portal:<br/>
https://portal.azure.com/#view/Microsoft_Azure_Marketplace/GalleryItemDetailsBladeNopdl/id/redfly.redfly-offer-1/selectionMode~/false/resourceGroupId//resourceGroupLocation//dontDiscardJourney~/false/selectedMenuId/home/launchingContext~/%7B%22galleryItemId%22%3A%22redfly.redfly-offer-1dpssp%22%2C%22source%22%3A%5B%22GalleryFeaturedMenuItemPart%22%2C%22VirtualizedTileDetails%22%5D%2C%22menuItemId%22%3A%22home%22%2C%22subMenuItemId%22%3A%22Search%20results%22%2C%22telemetryId%22%3A%22d25c8cae-a836-4037-a2dd-9cd2f8e9e001%22%7D/searchTelemetryId/2783b73c-f204-4ae8-a1f7-b5bb83db4e73
