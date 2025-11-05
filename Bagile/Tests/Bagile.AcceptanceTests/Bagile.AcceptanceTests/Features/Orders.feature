Feature: Order Management
  As an AI agent
  I want to query and retrieve order information
  So that I can answer business questions about sales and customers

Background:
  Given the database is clean
  And the following orders exist:
    | ExternalId | Status    | TotalAmount | CustomerEmail          | CustomerCompany     | OrderDate  |
    | 12243      | completed | 2520.00     | henry@themdu.com       | MDU Services Ltd    | 2025-10-23 |
    | 12244      | pending   | 1050.00     | alice@example.com      | Example Corp        | 2025-10-24 |
    | 12245      | completed | 950.00      | bob@anotherco.com      | Another Co          | 2025-10-25 |

Scenario: Retrieve all orders
  When I request all orders
  Then the response status should be 200
  And the response should contain 3 orders

Scenario: Filter orders by status
  When I request orders with status "completed"
  Then the response status should be 200
  And the response should contain 2 orders
  And all orders should have status "completed"

Scenario: Filter orders by date range
  When I request orders from "2025-10-24" to "2025-10-25"
  Then the response status should be 200
  And the response should contain 2 orders
  And all orders should be within the date range

Scenario: Filter orders by customer email
  When I request orders for email "henry@themdu.com"
  Then the response status should be 200
  And the response should contain 1 order
  And the order should have customer email "henry@themdu.com"

Scenario: Retrieve a specific order by ID
  Given an order exists with external ID "12243"
  When I request the order by its internal ID
  Then the response status should be 200
  And the order should have:
    | Field          | Value               |
    | ExternalId     | 12243               |
    | Status         | completed           |
    | TotalAmount    | 2520.00             |
    | CustomerEmail  | henry@themdu.com    |
    | CustomerCompany| MDU Services Ltd    |

Scenario: Retrieve order with enrolments
  Given an order exists with external ID "12243"
  And the order has the following enrolments:
    | StudentEmail           | CourseName   |
    | khalil@themdu.com      | PSM Advanced |
    | abiodun@themdu.com     | PSM Advanced |
  When I request the order by its internal ID
  Then the response status should be 200
  And the order should have 2 enrolments
  And the enrolments should include:
    | StudentEmail      | CourseName   |
    | khalil@themdu.com | PSM Advanced |

Scenario: Request non-existent order
  When I request order with ID 99999
  Then the response status should be 404
  And the response should contain an error message

Scenario: Pagination works correctly
  Given 25 orders exist in the system
  When I request orders with page 1 and page size 10
  Then the response should contain 10 orders
  And the pagination info should show:
    | Field       | Value |
    | Page        | 1     |
    | PageSize    | 10    |
    | TotalCount  | 28    |
    | TotalPages  | 3     |
    | HasNextPage | true  |

Scenario: Query orders with multiple filters
  When I request orders with:
    | Parameter | Value               |
    | status    | completed           |
    | from      | 2025-10-23          |
    | to        | 2025-10-25          |
    | email     | henry@themdu.com    |
  Then the response status should be 200
  And the response should contain 1 order
  And the order should match all filter criteria