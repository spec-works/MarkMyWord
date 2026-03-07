Identity Template Spec

PM Owner:   
Eng Owner:     
Epic(s): 

# One-pager section

### What: Introduction

What is this feature? Is this a differentiator or table stakes?  What problem does this solve?  This is your elevator pitch, so it should be short and to the point. Later sections will drill into supporting details, but this is the core explanation for what you’re proposing. This summary should also be included in the Epic as “description” text representing this scenario. Consider if high level sketches would help land your point.

Specific example: “Tenant Friending empowers one organization to trust and provide resource access to members of another organization. It enables Complex org scenarios like mergers and acquisitions, and is a high priority request for Microsoft365.”

### Who: Who is the customer and what is their need we’re addressing?

Who is the customer for this scenario? Provide a list of the needs or problems the customer is facing that this scenario will address. How will we know we have addressed them?  Include the customer’s market segment or licensing model if it would be relevant to understand the feature. The human-centered design checklist at [http://aka.ms/EDICT](rId12) and the tools at [http://aka.ms/designrevolution](rId13) can help you learn how to engage with your customers effectively.

This is also a good place to call out what an MVP would look like and how subsequent releases can build on top of it.

### Why: Business outcomes, Chapter OKR

Include top 1-3 outcomes from this feature that are relevant to the business (e.g. usage), performance (e.g. scale/reliability) or compete (e.g. market share acquisition or differential growth w.r.t. competitor).  These should remain very high level, with detailed metrics tracked in the [Metrics](rId14) section below.  Note that these are Microsoft-facing, any user facing OKRs should be part of the functional requirements below.

Example: 1M users within 3 months of deployment. 5 preview customers with an NPS index of >X. 

## Scenarios 

Short narrative of how the feature will be used. Should not address HOW the problem is solved, but should outline the person’s emotional state and expectations, and what their desired end state is. You probably have multiple personas who have a role to play here, so make sure you’re thinking about all of them (end user, developer, and IT Admin). Feel free to split further if it makes sense, for example if you want to have separate scenarios for global admins vs. app admins, and if you’re providing different scenarios for MVP vs. nice-to-haves you should clearly indicate that.

Example: “Sarah is a developer at Contoso who is building a new web app and wants to allow AAD and MSA users to sign into it so she can call Microsoft Graph to get information about them. She knows she can use MSAL to achieve this because she found documentation which pointed her to a sample that walks through all the steps to set up her app, and she thinks the instructions look clear and easy enough to accomplish.  She gets sets aside a day to get the app working end to end, but when she rolls up her sleeves and digs in it only takes her an hour to work through the sample and handle the common error cases.  Since she has extra time, she decides to build another feature she’d been considering, and her boss is extremely happy with her progress and asks her to give a short presentation to the team about her experience coding against Microsoft Graph.”

### End User

How would an end user experience this feature?  Does it result in more access denied experiences?  Is there in-context information for them (since end users generally are not aware of announcements of new features)? List out the top-level use cases. 

### Developer

Consider both API access for the feature (can it be scripted?) as well as how this feature will impact current and future apps.  For example, a feature that adds a new potential error code to authentication flows could break existing apps that are not expecting it. List out the top-level use cases. 

### IT

Consider how an admin might want to regulate this feature.  Do they need to control who can use it, or possible values for it?  What role(s) is this exposed to? List out the top-level use cases and admin personas who would get value from or need to control this feature.

### Support Engineer

Consider how a support engineer might want to document and help unblock or support this feature. Do they have access and understanding required to unblock customers and reduce friction for customer adoption?   

## Out of scope

These are things that people might expect to find in this spec but we have chosen not to do for this feature. This should be used to help bound the problem space to something solvable in the given half-year.

Example: Doing awesome extra feature – Although that really would be an awesome feature, it doesn’t fit into the scope for this feature because we need to prove the opportunity first and awesome feature will be considered for future prioritization if this initial release is a success.

## Features and Partner teams/partner dependencies

Who are the key partners that needs to be informed, consulted, or otherwise there is a mutual dependency that should be flagged at PM/spec time.  This list will likely need to be updated as you develop the details of the design, but some dependencies will be obvious at the one-pager stage and should be listed as soon as they’re identified. Engineering teams are responsible for managing dependencies. However, it is appropriate for the spec to capture major dependencies on other teams. It should include dependencies within and outside the Identity division (e.g. Office, Windows E&S)

| Feature | Identity Team / Contacts | 1P Partner Team / Contacts | What is required with them? | ADO item for tracking |
| --- | --- | --- | --- | --- |
|  |  | Team (Quarter expected) |  |  |
|  |  |  |  |  |
|  |  |  |  |  |
|  |  |  |  |  |
|  |  |  |  |  |
|  |  |  |  |  |

## Open questions

Any large open issues that will need to be answered as part of the final design should be listed here.  Include information about why those questions are relevant to any future decisions.  There are open/resolved question styles included in the template that you can use throughout the doc as you flesh out the spec.

Example open question

Example resolved question

----------------------------- Stop here for one pager---------------------------

---



# Spec properties

| Stakeholders |
| --- |
| ADO Item |
| API Spec When designing the API for this feature use the [API spec template](rId15). |
| UI flows You can use the [UI toolkits and processes](rId16) to guide you through developing this |
| Other related documents |

# Requirements

## Use cases / UX flows / API functionality

Use this section to provide detailed flows of the user experience, flow charts, or use cases. If you will be A/B testing, include information about how the test will be run and the criteria you’ll use to declare success of a variation. If you are designing an API, CLI or SDK, you may want to provide an overview of the schema for the API, snippets describing how the API will be used, or a high-level overview of the API shape.

You should add sub-sections as needed.  This will likely be quite long when fully fleshed out.

## Metrics / OKRs

List metrics you’ll use to measure the success of the feature. These will typically be more quantitative than qualitative and cover things like performance, usage/adoption, NPS, reliability, number of support cases, number of app/user pairs, etc.  These should be tied to your goals. Metrics details will also drive what telemetry points you will need. 

Example: P90 call latency for GET /me/metrics <200ms >

| Objective | Key Results |
| --- | --- |
|  |  |
|  |  |
|  |  |
|  |  |

## Functional Requirements

Use this section to provide detailed requirements about what the feature will do. Sub-bullets should be used to provide informative information to help explain the requirement. Group requirements into additional sections as appropriate to improve ease of consumption, such as by functional areas or user type.  The goal is to make logical groupings that can be reasoned across, so aim for groupings that lead to less than 10-20 requirements per section.

Please refer to the [PM Checklist OneNote](rId17) ([Web view](rId18)) for guidance on additional requirements you might not have considered.

### <General/Telemetry> requirements

Copy/paste this section as needed to create additional requirement groupings.

Example: Users can log into the Ibiza portal and see all of their B2C apps in the same view as other apps. 

| # | Requirement | Priority |
| --- | --- | --- |
|  |  | P1 |

# Additional requirements

These are things that all features should consider, although not all sections will be applicable to all features.  
NOTE: this is NOT the FULL checklist (add link) 

| Consideration | Evaluated | Notes |
| --- | --- | --- |
| Manageability – RBAC and API support How is the feature exposed via APIs and PowerShell to enable scripting?  How is end-user usage of the feature controlled? How is management of the feature delegated beyond the global admin? (NOTE: nothing should require global admin) For help designing APIs to support your feature, reach out to Dan Kershaw ([dkershaw](rId19)). For help designing the control of usage and management, see [https://aka.ms/aadrbaconboarding](rId20). |  |
| Documentation Includes API reference, samples, blog posts, feature announcements, etc.  What is needed to help customers know your feature exists and how to use it?  Include developer, IT, and end user documentation requirements. Example: Docs X, Y, and Z and samples A, B, and C need to be updated to include <foo> and we need to create an additional document explaining the concept of <bar>. |  |
| Licensing model Is your feature free or part of a paid SKU? If paid, how are you enabling developers to code and test against it for free? What happens to users’ data when they stop paying for the license? |  |
| Supportability Does Support know this feature is shipping? What errors are customers likely to encounter in this feature?  What does Support need to know to help them debug and resolve their issues? |  |
| National Clouds Will this feature be available in all sovereign clouds?  Does it require special management or training for operators?  What differences (if any) will users is sovereign clouds experience? |  |
| Security How does this feature alter the threat model for your area? Are there specific security considerations that need to be addressed in the design of this feature? |  |
| Performance How will this feature impact the performance of the product?  Will it stay performant at scale?  How will you measure it? |  |
| Privacy Will this feature collect new data about the user? How will users control how their data is used? Will this data change the GDPR regulations which govern your feature? |  |
| Accessibility Does your feature include a user experience? Are there any special considerations you need to make for users with disabilities?  Have you scheduled an accessibility test pass? |  |
| Usability Are your users and scenarios based in real-world data? Have your user flows and UI designs been evaluated for usability issues? See [http://aka.ms/idusable](rId21) for self-serve usability resources and information about how to engage with the Identity Usability team. |  |
| Geopolitical Are there any special considerations for this feature in other locales?  Examples of areas that might need more consideration are iconography (hearts are sometimes offensive/carry only romantic meaning) or interpersonal workflows (some cultures have a strong hierarchy and certain requests or language are unacceptable from a subordinate). Are you introducing new strings to the product? Please ensure you have reviewed these using Policheck. [https://microsoft.sharepoint.com/teams/celaGlobalReadiness/Pages/PoliCheck.aspx](rId22) |  |
| Previews/Rollout How will customers be invited to the private preview?  What are the criteria for transitioning to public preview? GA? |  |
| Marketing plan Does this feature need any blogs or videos to support its release?  How would someone demo your feature for a customer or at a conference? |  |


# Document History

Use this section to track major changes to the doc so folks who review this document multiple times know where they should focus their attention.

Example: 21 May 2018: Ready for initial review of Page 1 section

| Date | Description |
| --- | --- |
|  |  |




