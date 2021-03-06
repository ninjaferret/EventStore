#include "stdafx.h"
#include "CompiledScript.h"
#include "PreludeScript.h"
#include "QueryScript.h"
#include "EventHandler.h"

#include <string>


using namespace v8;

namespace js1 
{

	QueryScriptScope::QueryScriptScope(QueryScript *query_script) 
	{
		current = query_script;
	};

	QueryScriptScope::~QueryScriptScope() 
	{
		current = NULL;
	};

	QueryScript &QueryScriptScope::Current() 
	{
		return *QueryScriptScope::current;
	};

	THREADSTATIC QueryScript * QueryScriptScope::current;

	QueryScript::~QueryScript()
	{
		for (std::list<EventHandler *>::iterator it = registred_handlers.begin(); it != registred_handlers.end(); it++)
		{
			delete *it;
		}
	}

	bool QueryScript::compile_script(const uint16_t *script_source, const uint16_t *file_name)
	{
		this->register_command_handler_callback = register_command_handler_callback;

		return CompiledScript::compile_script(script_source, file_name);

	}

	v8::Handle<v8::Value> QueryScript::run() 
	{
		QueryScriptScope scope(this);
		return run_script(get_context());
	}

	v8::Persistent<v8::String> QueryScript::execute_handler(void *event_handler_handle, const uint16_t *data_json, const uint16_t *data_other[], int32_t other_length) 
	{
		EventHandler *event_handler = reinterpret_cast<EventHandler *>(event_handler_handle);

		v8::HandleScope handle_scope;
		v8::Context::Scope local(get_context());
		QueryScriptScope queryScope(this);

		v8::Handle<v8::String> data_json_handle = v8::String::New(data_json);
		v8::Handle<v8::Value> argv[10];
		argv[0] = data_json_handle;

		for (int i = 0; i < other_length; i++) {
			v8::Handle<v8::String> data_other_handle = v8::String::New(data_other[i]);
			argv[1 + i] = data_other_handle;
		}

		v8::Handle<v8::Object> global = get_context()->Global();

		v8::TryCatch try_catch;
		v8::Handle<v8::Value> result = event_handler->get_handler()->Call(global, 1 + other_length, argv);
		set_last_error(result.IsEmpty(), try_catch);
		v8::Handle<v8::String> empty;
		if (result.IsEmpty())
		{
			return v8::Persistent<v8::String>::New(empty);
		}
		if (!result->IsString()) {
			set_last_error(v8::String::New("Handler must return string data"));
			return v8::Persistent<v8::String>::New(empty);
		}
		return v8::Persistent<v8::String>::New(result.As<v8::String>());
	}

	v8::Persistent<v8::ObjectTemplate> QueryScript::create_global_template()
	{
		v8::Persistent<v8::Context> temp_context = v8::Context::New();
		v8::Context::Scope temp_context_scope(temp_context);

		v8::Handle<v8::Value> query_script_wrap = v8::External::Wrap(this);

		std::vector<v8::Handle<v8::Value> > arguments(2);
		arguments[0] = v8::FunctionTemplate::New(on_callback, query_script_wrap)->GetFunction();
		arguments[1] = v8::FunctionTemplate::New(notify_callback, query_script_wrap)->GetFunction();

		v8::Persistent<v8::ObjectTemplate> result = prelude->get_template(arguments);
		temp_context.Dispose();
		return result;
	}

	v8::Handle<v8::Value> QueryScript::on(const v8::Arguments& args) 
	{
		if (args.Length() != 2) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'on' handler expects 2 arguments")));

		if (args[0].IsEmpty() || args[1].IsEmpty()) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'on' handler argument cannot be empty")));

		if (!args[0]->IsString()) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'on' handler first argument must be a string")));

		if (!args[1]->IsFunction()) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'on' handler second argument must be a function")));

		v8::Handle<v8::String> name(args[0].As<v8::String>());
		v8::Handle<v8::Function> handler(args[1].As<v8::Function>());
		EventHandler *event_handler = new EventHandler(name, handler);
		registred_handlers.push_back(event_handler);
		v8::String::Value uname(name);
		this->register_command_handler_callback(*uname, event_handler);
		return v8::Undefined();
	}

	v8::Handle<v8::Value> QueryScript::notify(const v8::Arguments& args) 
	{
		if (args.Length() != 2) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'notify' handler expects 2 arguments")));

		if (args[0].IsEmpty() || args[1].IsEmpty()) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'notify' handler argument cannot be empty")));

		if (!args[0]->IsString()) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'notify' handler first argument must be a string")));

		if (!args[1]->IsString()) 
			return v8::ThrowException(v8::Exception::Error(v8::String::New("The 'notify' handler second argument must be a string")));

		v8::Handle<v8::String> name(args[0].As<v8::String>());
		v8::Handle<v8::String> body(args[1].As<v8::String>());

		v8::String::Value name_value(name);
		v8::String::Value body_value(body);

		this->reverse_command_callback(*name_value, *body_value);

		return v8::Undefined();
	}

	v8::Handle<v8::Value> QueryScript::on_callback(const v8::Arguments& args) 
	{
		v8::Handle<v8::Value> data = args.Data();
		QueryScript *query_script = reinterpret_cast<QueryScript *>(v8::External::Unwrap(data));
		return query_script->on(args);
	};

	v8::Handle<v8::Value> QueryScript::notify_callback(const v8::Arguments& args) 
	{
		v8::Handle<v8::Value> data = args.Data();
		QueryScript *query_script = reinterpret_cast<QueryScript *>(v8::External::Unwrap(data));
		return query_script->notify(args);
	};


}

