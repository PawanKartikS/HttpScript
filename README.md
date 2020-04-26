### Nebula
Nebula is a scripting language purely used for calling and accessing REST APIs. It is written in C# with no dependencies and compiles cleanly to a single cross-platform binary.


### Accessing a simple REST API
```py
def fetch_data(api_endpoint):
	# Not specifying method defaults to GET while timeout defaults to 5000ms
	api my_api = {endpoint: api_endpoint, method: GET}
	res my_res = my_api::res          # Access the API's response		

	return my_res
end

def Main:
	use warns
	use scope
	
	var api_endpoint = 'https://reqres.in/api/users'
	var api_data = main.fetch_data(api_endpoint)
	
	# If the last API call was success
	if success
		print api_data
	else
		print 'Could not fetch data from the API'
	end
end
```

You can access the API call's error code as -
```py
err my_api_err = my_res::err
if my_api_err != 'OK'
    # There was an error calling the API
end
```

Nebula is written in C#. Any API calls specified are executed by CSharp's `WebRequest` engine. Nebula compiles down to an extremely small binary that is capable of running on platforms that support C#. This allows you to bundle the binary with your scripts.

Nebula works in 2 phases -

* Phase 1. Syntax is checked for errors and Abstract Syntax Tree is generated for the written code.

* Phase 2. Evaluations are performed and values are substituted wherever necessary. The code is then executed.

Nebula is still in it's early stages. Expect things to change as the code matures.
