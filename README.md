# Spotify Console App

I want to make a whole workout companion app. So far I can create a playlist for the user and fill it with a specific amount of songs of a specific genre both of which are specified by the user. 

## Issues Encountered and Resolutions

### Issue 1: Genre Search Functionality
- **Description**: Had difficulty implementing the genre search functionality. The application was unable to retrieve tracks based on the specified genre.
- **Steps Taken**:
  - Reviewed the code for any syntax errors or logical issues.
  - Double-checked the API documentation to ensure correct usage of the search parameters.
- **Resolution**: Identified that the search URL was not properly constructed, leading to incorrect genre filtering. Implemented proper URL construction using Uri.EscapeDataString() method to encode the genre parameter correctly.

### Issue 2: Authentication Errors
- **Description**: Encountered errors during the authentication process, such as mismatched states and failed token retrieval.
- **Steps Taken**:
  - Implemented secure state generation and validation to ensure proper authorization code exchange.
  - Reviewed the authorization flow to identify any potential vulnerabilities or misconfigurations.
- **Resolution**: Successfully resolved authentication errors by refining the authorization code exchange process and implementing proper error handling mechanisms.

### Issue 3: HTTP Request Errors
- **Description**: Faced issues with sending HTTP requests to the Spotify API and handling responses.
- **Steps Taken**:
  - Implemented robust error handling for HTTP requests using try-catch blocks and HttpClient's error handling features.
  - Analyzed the HTTP response status codes to identify the root cause of the errors.
- **Resolution**: Addressed HTTP request errors by refining the error handling mechanisms and ensuring appropriate handling of various response status codes.

### Issue 4: Access Token Retrieval Failures
- **Description**: Experienced difficulties retrieving access tokens from the Spotify API.
- **Steps Taken**:
  - Implemented proper authorization code exchange and error handling mechanisms to address token retrieval failures.
  - Reviewed the authorization process to ensure that all required parameters were correctly provided.
- **Resolution**: Successfully resolved access token retrieval failures by refining the authorization code exchange process and implementing proper error handling mechanisms.

### Issue 5: API Endpoint Access Problems
- **Description**: Encountered challenges accessing specific API endpoints, such as retrieving top tracks or artists.
- **Steps Taken**:
  - Reviewed the API documentation carefully and adjusted endpoint URLs and parameters as needed.
  - Tested API endpoint access thoroughly to verify correct usage of permissions and scopes.
- **Resolution**: Resolved API endpoint access problems by adjusting endpoint URLs and parameters according to the API documentation and ensuring proper authentication and authorization.

### Issue 6: JSON Parsing Errors
- **Description**: Faced errors when parsing JSON responses from the Spotify API, including deserialization issues.
- **Steps Taken**:
  - Utilized the Newtonsoft.Json library for robust JSON parsing and deserialization.
  - Implemented proper error handling mechanisms to manage parsing errors and handle unexpected data structures.
- **Resolution**: Successfully resolved JSON parsing errors by implementing robust JSON parsing logic and proper error handling mechanisms.

### Issue 7: Error Handling Strategies
- **Description**: Developed strategies to handle various types of errors encountered during API interactions.
- **Steps Taken**:
  - Developed comprehensive error handling mechanisms to gracefully handle HTTP errors, parsing errors, and authentication failures.
  - Categorized errors based on severity and implemented appropriate error messages or actions for each category.
- **Resolution**: Successfully implemented comprehensive error handling strategies to ensure smooth user experience and mitigate potential issues.

### Issue 8: Debugging and Troubleshooting
- **Description**: Engaged in debugging and troubleshooting activities to identify and resolve issues encountered during development.
- **Steps Taken**:
  - Utilized debugging tools, logging, and systematic troubleshooting approaches to diagnose and address issues effectively.
  - Documented debugging steps and findings to track progress and facilitate collaboration.
- **Resolution**: Leveraged debugging and troubleshooting techniques to identify and resolve issues effectively, ensuring the stability and reliability of the application.

### Issue 9: BIG PROBLEM I BROKE ALL MY CODE! Having to restart from the previous implementation. :(

