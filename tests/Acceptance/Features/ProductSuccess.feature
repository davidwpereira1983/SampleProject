Feature: ProductSuccess

Scenario: As a user, I want to be able to get the list of products
	Given The user is logged in to the platform
	When Get the list of products
	Then the platform return the products:
		| Name      |
		| Product01 |
		| Product02 |
		| Product03 |