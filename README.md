# redfly.ai
redfly.ai lets you synchronize your database with Redis <i>transparently</i> and <i>generate</i> a data access layer that <i>integrates</i> data access code with caching. This open-source repo is intended to make it easy for developers to understand and try out our system.

**Goals**

Provide source code that:

1. Let you easily verify that our system performs better than conventional techniques for data access at scale (This is v1 - this is ready to go & will be published soon).
2. Provide a way for anybody to test our Redis synchronization service from anywhere on-demand (TBD).
3. Provide a way for anybody to generate their data access backend services on our cloud on-demand (TBD).

Even though these are intended as fully-functional demos, items 2 & 3 are quite ambitious and complex to implement. Currently, we do all this setup work for our customers within our environment. This should let anyone get a taste of our technology without manual work from our part.

No complex configuration or modifications are intended for these applications to work. These will be simple console applications written in C# and the latest version of .NET Core available. 

**Compatibility**

We currently support SQL Server, Redis, Azure Search & Azure cloud. We intend to support other relational databases in the future (like Postgres). Eventually, we plan to support all disk based databases, and other public clouds like AWS & GCP. 

_We have a list of customers who are waiting for Postgres support. If you are interested, please let us know at: developer at redfly dot ai_.

**Pre-requisites**

Authentication is necessary to connect to our cloud services hosted on Azure. You can register with us here:<br/>
https://transparent.azurewebsites.net/Identity/Account/Register

Check your junk folder for emails from redfly.ai. Since this is a new domain, emails will go to the junk folder.

All our cloud services run on our technology.  

**Documentation**

More details can be found here:<br/>
Higher-level overview: https://redfly.ai <br/>
More technical: https://nautilus2k.netlify.app <br/>

**Sales**

Companies interested in using our platform directly can find more details here:<br/>
https://azuremarketplace.microsoft.com/en-us/marketplace/apps/redfly.redfly-offer-1?tab=overview

If you have access to the Azure Portal, you can find us here:<br/>
https://portal.azure.com/#view/Microsoft_Azure_Marketplace/GalleryItemDetailsBladeNopdl/id/redfly.redfly-offer-1/selectionMode~/false/resourceGroupId//resourceGroupLocation//dontDiscardJourney~/false/selectedMenuId/home/launchingContext~/%7B%22galleryItemId%22%3A%22redfly.redfly-offer-1dpssp%22%2C%22source%22%3A%5B%22GalleryFeaturedMenuItemPart%22%2C%22VirtualizedTileDetails%22%5D%2C%22menuItemId%22%3A%22home%22%2C%22subMenuItemId%22%3A%22Search%20results%22%2C%22telemetryId%22%3A%22d25c8cae-a836-4037-a2dd-9cd2f8e9e001%22%7D/searchTelemetryId/2783b73c-f204-4ae8-a1f7-b5bb83db4e73
