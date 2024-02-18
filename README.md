# Spotify
Spotify Console App
This console application interacts with the Spotify API to retrieve top tracks and artists for a user. At least so far that's all it does. I want to make a whole workout companion app. BUT SO FAR I CAN PRINT OUT USER DATA!

Issues Encountered
1. URL Parsing Challenges
Description: Had difficulty ensuring proper formatting and encoding of URLs for API requests.
Resolution: Implemented proper URL construction and encoding using Uri.EscapeDataString() method.
2. Authentication Errors
Description: Encountered errors during the authentication process, such as mismatched states and failed token retrieval.
Resolution: Implemented error handling mechanisms to manage authentication failures gracefully.
3. HTTP Request Errors
Description: Faced issues with sending HTTP requests to the Spotify API and handling responses.
Resolution: Implemented robust error handling for HTTP requests using try-catch blocks and HttpClient's error handling features.
4. Access Token Retrieval Failures
Description: Experienced difficulties retrieving access tokens from the Spotify API.
Resolution: Implemented proper authorization code exchange and error handling mechanisms to address token retrieval failures.
5. API Endpoint Access Problems
Description: Encountered challenges accessing specific API endpoints, such as retrieving top tracks or artists.
Resolution: Reviewed API documentation and adjusted endpoint URLs and parameters to resolve access issues.
6. Authorization Code Handling
Description: Faced with challenges related to handling authorization codes securely and efficiently.
Resolution: Implemented secure state generation and validation to ensure proper authorization code exchange.
7. JSON Parsing Errors
Description: Encountered errors when parsing JSON responses from the Spotify API, including deserialization issues.
Resolution: Implemented robust JSON parsing logic using Newtonsoft.Json library and proper error handling mechanisms.
8. Error Handling Strategies
Description: Developed strategies to handle various types of errors encountered during API interactions.
Resolution: Implemented comprehensive error handling mechanisms to gracefully handle HTTP errors, parsing errors, and authentication failures.
9. Debugging and Troubleshooting
Description: Engaged in debugging and troubleshooting activities to identify and resolve issues encountered during development.
Resolution: Leveraged debugging tools, logging, and systematic troubleshooting approaches to diagnose and address issues effectively.
